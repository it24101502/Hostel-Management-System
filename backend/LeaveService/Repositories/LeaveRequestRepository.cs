using LeaveService.Models;
using MySqlConnector;

namespace LeaveService.Repositories;

public class LeaveRequestRepository : ILeaveRequestRepository
{
    private const string SelectColumns = """
        leave_request_id,
        student_user_id,
        student_username,
        departure_date,
        expected_return_date,
        reason,
        companion_name,
        companion_relationship,
        companion_phone,
        status,
        created_at,
        updated_at
        """;

    private readonly string _connectionString;

    public LeaveRequestRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task<LeaveRequest> CreateWithNotificationAsync(
        NewLeaveRequest request,
        NewLeaveNotification notification,
        DateTime occurredAtUtc)
    {
        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        ulong leaveRequestId;

        try
        {
            leaveRequestId =
                await InsertRequestAsync(
                    connection,
                    transaction,
                    request);

            await InsertAuditAsync(
                connection,
                transaction,
                leaveRequestId,
                request.StudentUserId,
                LeaveRoles.Student,
                LeaveAuditActions.Submit,
                null,
                LeaveRequestStatuses.Pending,
                null,
                occurredAtUtc);

            await InsertNotificationAsync(
                connection,
                transaction,
                leaveRequestId,
                notification,
                occurredAtUtc);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return await GetByIdAsync(leaveRequestId)
            ?? throw new InvalidOperationException(
                "The leave request was saved but could not be loaded.");
    }

    public async Task<LeaveRequest?> GetByIdAsync(
        ulong leaveRequestId)
    {
        string query = $"""
            SELECT
                {SelectColumns}
            FROM leave_requests
            WHERE leave_request_id = @leaveRequestId
            LIMIT 1;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@leaveRequestId",
            leaveRequestId);

        await using var reader =
            await command.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? MapRequest(reader)
            : null;
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetByStudentAsync(
        ulong studentUserId)
    {
        string query = $"""
            SELECT
                {SelectColumns}
            FROM leave_requests
            WHERE student_user_id = @studentUserId
            ORDER BY created_at DESC, leave_request_id DESC;
            """;

        var requests = new List<LeaveRequest>();

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
            requests.Add(MapRequest(reader));
        }

        return requests;
    }

    private static async Task<ulong> InsertRequestAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        NewLeaveRequest request)
    {
        const string query = """
            INSERT INTO leave_requests
            (
                student_user_id,
                student_username,
                departure_date,
                expected_return_date,
                reason,
                companion_name,
                companion_relationship,
                companion_phone,
                status
            )
            VALUES
            (
                @studentUserId,
                @studentUsername,
                @departureDate,
                @expectedReturnDate,
                @reason,
                @companionName,
                @companionRelationship,
                @companionPhone,
                @status
            );
            """;

        await using var command =
            new MySqlCommand(query, connection, transaction);

        command.Parameters.AddWithValue(
            "@studentUserId",
            request.StudentUserId);

        command.Parameters.AddWithValue(
            "@studentUsername",
            request.StudentUsername);

        command.Parameters.AddWithValue(
            "@departureDate",
            request.DepartureDate.ToDateTime(
                TimeOnly.MinValue));

        command.Parameters.AddWithValue(
            "@expectedReturnDate",
            request.ExpectedReturnDate.ToDateTime(
                TimeOnly.MinValue));

        command.Parameters.AddWithValue(
            "@reason",
            request.Reason);

        command.Parameters.AddWithValue(
            "@companionName",
            request.CompanionName);

        command.Parameters.AddWithValue(
            "@companionRelationship",
            request.CompanionRelationship);

        command.Parameters.AddWithValue(
            "@companionPhone",
            request.CompanionPhone);

        command.Parameters.AddWithValue(
            "@status",
            LeaveRequestStatuses.Pending);

        await command.ExecuteNonQueryAsync();

        return checked((ulong)command.LastInsertedId);
    }

    private static async Task InsertAuditAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        ulong leaveRequestId,
        ulong? actorUserId,
        string actorRole,
        string action,
        string? fromStatus,
        string toStatus,
        string? remarks,
        DateTime occurredAtUtc)
    {
        const string query = """
            INSERT INTO leave_request_audit_logs
            (
                leave_request_id,
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
                @leaveRequestId,
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
            "@leaveRequestId",
            leaveRequestId);

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

    private static async Task InsertNotificationAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        ulong leaveRequestId,
        NewLeaveNotification notification,
        DateTime createdAtUtc)
    {
        const string query = """
            INSERT INTO leave_notifications
            (
                leave_request_id,
                notification_type,
                message,
                created_at
            )
            VALUES
            (
                @leaveRequestId,
                @notificationType,
                @message,
                @createdAt
            );
            """;

        await using var command =
            new MySqlCommand(query, connection, transaction);

        command.Parameters.AddWithValue(
            "@leaveRequestId",
            leaveRequestId);

        command.Parameters.AddWithValue(
            "@notificationType",
            notification.NotificationType);

        command.Parameters.AddWithValue(
            "@message",
            notification.Message);

        command.Parameters.AddWithValue(
            "@createdAt",
            createdAtUtc);

        await command.ExecuteNonQueryAsync();
    }

    private static LeaveRequest MapRequest(
        MySqlDataReader reader)
    {
        return new LeaveRequest
        {
            LeaveRequestId =
                Convert.ToUInt64(reader["leave_request_id"]),

            StudentUserId =
                Convert.ToUInt64(reader["student_user_id"]),

            StudentUsername =
                Convert.ToString(reader["student_username"])!,

            DepartureDate =
                DateOnly.FromDateTime(
                    reader.GetDateTime("departure_date")),

            ExpectedReturnDate =
                DateOnly.FromDateTime(
                    reader.GetDateTime("expected_return_date")),

            Reason = Convert.ToString(reader["reason"])!,

            CompanionName =
                Convert.ToString(reader["companion_name"])!,

            CompanionRelationship =
                Convert.ToString(reader["companion_relationship"])!,

            CompanionPhone =
                Convert.ToString(reader["companion_phone"])!,

            Status = Convert.ToString(reader["status"])!,

            // MySQL returns DATETIME without a time zone. The
            // service stores UTC, so mark the value as UTC so it is
            // serialised with a trailing "Z" and browsers convert
            // it to the viewer's local time correctly.
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
}
