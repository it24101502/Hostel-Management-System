using ComplaintService.Models;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using ComplaintService.DTOs;

namespace ComplaintService.Repositories;

public class ComplaintRepository : IComplaintRepository
{
    private const string SelectColumns = """
        complaint_id,
        student_user_id,
        student_username,
        category,
        description,
        status,
        assigned_to_user_id,
        assigned_at,
        resolved_at,
        created_at,
        updated_at
        """;

    private readonly string _connectionString;

    public ComplaintRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task<Complaint> CreateAsync(
        NewComplaint complaint,
        DateTime occurredAtUtc)
    {
        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        ulong complaintId;

        try
        {
            complaintId =
                await InsertComplaintAsync(
                    connection,
                    transaction,
                    complaint,
                    occurredAtUtc);

            await InsertAuditAsync(
                connection,
                transaction,
                complaintId,
                complaint.StudentUserId,
                ComplaintRoles.Student,
                ComplaintAuditActions.Submit,
                null,
                ComplaintStatuses.Open,
                null,
                occurredAtUtc);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return await GetByIdAsync(complaintId)
            ?? throw new InvalidOperationException(
                "The complaint was saved but could not be loaded.");
    }

    public async Task<Complaint?> GetByIdAsync(
        ulong complaintId)
    {
        string query = $"""
            SELECT
                {SelectColumns}
            FROM complaints
            WHERE complaint_id = @complaintId
            LIMIT 1;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@complaintId",
            complaintId);

        await using var reader =
            await command.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? MapComplaint(reader)
            : null;
    }

    public async Task<IReadOnlyList<Complaint>> GetByStudentAsync(
        ulong studentUserId)
    {
        string query = $"""
            SELECT
                {SelectColumns}
            FROM complaints
            WHERE student_user_id = @studentUserId
            ORDER BY created_at DESC, complaint_id DESC;
            """;

        var complaints = new List<Complaint>();

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@studentUserId",
            studentUserId);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            complaints.Add(MapComplaint(reader));
        }

        return complaints;
    }

    public async Task<IReadOnlyList<Complaint>> GetFilteredAsync(
        string? status,
        string? category)
    {
        string query = $"""
            SELECT
                {SelectColumns}
            FROM complaints
            WHERE (@status IS NULL OR status = @status)
              AND (@category IS NULL OR category = @category)
            ORDER BY created_at DESC, complaint_id DESC;
            """;

        var complaints = new List<Complaint>();

        await using var connection =
            new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@status", (object?)status ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "@category", (object?)category ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            complaints.Add(MapComplaint(reader));
        }

        return complaints;
    }

    public async Task AssignAsync(
        ulong complaintId,
        ulong assigneeUserId,
        ulong actorUserId,
        string actorRole,
        DateTime occurredAtUtc)
    {
        const string updateQuery = """
            UPDATE complaints
            SET assigned_to_user_id = @assignee,
                assigned_at = @occurredAt
            WHERE complaint_id = @complaintId;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            string currentStatus;

            await using (var read = new MySqlCommand(
                "SELECT status FROM complaints WHERE complaint_id = @id;",
                connection, transaction))
            {
                read.Parameters.AddWithValue("@id", complaintId);
                currentStatus =
                    Convert.ToString(await read.ExecuteScalarAsync())
                    ?? throw new InvalidOperationException(
                        "The complaint no longer exists.");
            }

            await using (var update = new MySqlCommand(
                updateQuery, connection, transaction))
            {
                update.Parameters.AddWithValue("@assignee", assigneeUserId);
                update.Parameters.AddWithValue("@occurredAt", occurredAtUtc);
                update.Parameters.AddWithValue("@complaintId", complaintId);
                await update.ExecuteNonQueryAsync();
            }

            await InsertAuditAsync(
                connection, transaction, complaintId,
                actorUserId, actorRole,
                ComplaintAuditActions.Assign,
                currentStatus, currentStatus,
                $"Assigned to user {assigneeUserId}.",
                occurredAtUtc);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> TryChangeStatusAsync(
        ulong complaintId,
        string fromStatus,
        string toStatus,
        ulong actorUserId,
        string actorRole,
        string? remarks,
        DateTime occurredAtUtc)
    {
        const string updateQuery = """
            UPDATE complaints
            SET status = @toStatus,
                resolved_at = CASE WHEN @toStatus = 'RESOLVED'
                                   THEN @occurredAt ELSE NULL END
            WHERE complaint_id = @complaintId
              AND status = @fromStatus;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            int changedRows;

            await using (var update = new MySqlCommand(
                updateQuery, connection, transaction))
            {
                update.Parameters.AddWithValue("@toStatus", toStatus);
                update.Parameters.AddWithValue("@fromStatus", fromStatus);
                update.Parameters.AddWithValue("@occurredAt", occurredAtUtc);
                update.Parameters.AddWithValue("@complaintId", complaintId);
                changedRows = await update.ExecuteNonQueryAsync();
            }

            if (changedRows == 0)
            {
                await transaction.RollbackAsync();
                return false;
            }

            await InsertAuditAsync(
                connection, transaction, complaintId,
                actorUserId, actorRole,
                ComplaintAuditActions.StatusChange,
                fromStatus, toStatus, remarks, occurredAtUtc);

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task AddNotificationAsync(
        Guid notificationId,          // kept so the interface doesn't change; unused
        ulong complaintId,
        ulong studentId,
        string message,
        DateTime createdAtUtc)
    {
        const string sql = """
            INSERT INTO complaint_notifications
            (
                complaint_id,
                recipient_user_id,
                message,
                is_read,
                created_at
            )
            VALUES
            (
                @complaintId,
                @studentId,
                @message,
                0,
                @createdAt
            );
            """;

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@complaintId", complaintId);
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@message", message);
        command.Parameters.AddWithValue("@createdAt", createdAtUtc);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<StudentNotificationResponse>>
        GetNotificationsByStudentIdAsync(ulong studentId)
    {
        const string query = """
            SELECT
                notification_id   AS NotificationId,
                complaint_id      AS ComplaintId,
                recipient_user_id AS StudentId,
                message           AS Message,
                is_read           AS IsRead,
                created_at        AS CreatedAt
            FROM complaint_notifications
            WHERE recipient_user_id = @StudentId
            ORDER BY created_at DESC, notification_id DESC;
            """;

        var notifications = new List<StudentNotificationResponse>();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@StudentId", studentId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            notifications.Add(new StudentNotificationResponse
            {
                NotificationId = Convert.ToString(reader["NotificationId"])!,
                ComplaintId = Convert.ToUInt64(reader["ComplaintId"]),
                StudentId = Convert.ToUInt64(reader["StudentId"]),
                Message = Convert.ToString(reader["Message"])!,
                IsRead = Convert.ToBoolean(reader["IsRead"]),
                CreatedAt = DateTime.SpecifyKind(
                    reader.GetDateTime("CreatedAt"), DateTimeKind.Utc)
            });
        }

        return notifications;
    }

    private static async Task<ulong> InsertComplaintAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        NewComplaint complaint,
        DateTime occurredAtUtc)
    {
        const string query = """
            INSERT INTO complaints
            (
                student_user_id,
                student_username,
                category,
                description,
                status,
                created_at,
                updated_at
            )
            VALUES
            (
                @studentUserId,
                @studentUsername,
                @category,
                @description,
                @status,
                @createdAt,
                @createdAt
            );
            """;

        await using var command =
            new MySqlCommand(query, connection, transaction);

        command.Parameters.AddWithValue(
            "@studentUserId",
            complaint.StudentUserId);

        command.Parameters.AddWithValue(
            "@studentUsername",
            complaint.StudentUsername);

        command.Parameters.AddWithValue(
            "@category",
            complaint.Category);

        command.Parameters.AddWithValue(
            "@description",
            complaint.Description);

        command.Parameters.AddWithValue(
            "@status",
            ComplaintStatuses.Open);

        command.Parameters.AddWithValue(
            "@createdAt",
            occurredAtUtc);

        await command.ExecuteNonQueryAsync();

        return checked((ulong)command.LastInsertedId);
    }

    private static async Task InsertAuditAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        ulong complaintId,
        ulong? actorUserId,
        string actorRole,
        string action,
        string? fromStatus,
        string toStatus,
        string? remarks,
        DateTime occurredAtUtc)
    {
        const string query = """
            INSERT INTO complaint_audit_logs
            (
                complaint_id,
                actor_user_id,
                actor_role,
                action,
                from_status,
                to_status,
                remarks,
                occurred_at
            )
            VALUES
            (
                @complaintId,
                @actorUserId,
                @actorRole,
                @action,
                @fromStatus,
                @toStatus,
                @remarks,
                @occurredAt
            );
            """;

        await using var command =
            new MySqlCommand(query, connection, transaction);

        command.Parameters.AddWithValue(
            "@complaintId",
            complaintId);

        command.Parameters.AddWithValue(
            "@actorUserId",
            (object?)actorUserId ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@actorRole",
            actorRole);

        command.Parameters.AddWithValue(
            "@action",
            action);

        command.Parameters.AddWithValue(
            "@fromStatus",
            (object?)fromStatus ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@toStatus",
            toStatus);

        command.Parameters.AddWithValue(
            "@remarks",
            (object?)remarks ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@occurredAt",
            occurredAtUtc);

        await command.ExecuteNonQueryAsync();
    }

    private static Complaint MapComplaint(
        MySqlDataReader reader)
    {
        return new Complaint
        {
            ComplaintId =
                Convert.ToUInt64(reader["complaint_id"]),

            StudentUserId =
                Convert.ToUInt64(reader["student_user_id"]),

            StudentUsername =
                Convert.ToString(reader["student_username"])!,

            Category =
                Convert.ToString(reader["category"])!,

            Description =
                Convert.ToString(reader["description"])!,

            Status =
                Convert.ToString(reader["status"])!,

            AssignedToUserId =
                reader["assigned_to_user_id"] is DBNull
                    ? null
                    : Convert.ToUInt64(reader["assigned_to_user_id"]),

            AssignedAt =
                ReadNullableUtcDateTime(reader, "assigned_at"),

            ResolvedAt =
                ReadNullableUtcDateTime(reader, "resolved_at"),

            CreatedAt =
                DateTime.SpecifyKind(
                    reader.GetDateTime("created_at"),
                    DateTimeKind.Utc),

            UpdatedAt =
                DateTime.SpecifyKind(
                    reader.GetDateTime("updated_at"),
                    DateTimeKind.Utc)
        };
    }

    private static DateTime? ReadNullableUtcDateTime(
        MySqlDataReader reader,
        string column)
    {
        return reader[column] is DBNull
            ? null
            : DateTime.SpecifyKind(
                reader.GetDateTime(column),
                DateTimeKind.Utc);
    }
}