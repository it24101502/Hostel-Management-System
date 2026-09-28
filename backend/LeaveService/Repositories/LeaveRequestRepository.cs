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
        decided_by_user_id,
        decision_reason,
        decided_at,
        departure_recorded_by_user_id,
        actual_departure_at,
        return_recorded_by_user_id,
        actual_return_at,
        overdue_alerted_at,
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

    public async Task<IReadOnlyList<LeaveRequest>> GetFilteredAsync(
        LeaveRequestFilter filter,
        DateOnly today)
    {
        string query = $"""
            SELECT
                {SelectColumns}
            FROM leave_requests
            WHERE (@status IS NULL OR status = @status)
              AND (@fromDate IS NULL OR departure_date >= @fromDate)
              AND (@toDate IS NULL OR departure_date <= @toDate)
              AND (@overdueOnly = FALSE
                   OR (status = 'DEPARTED'
                       AND expected_return_date < @today))
            ORDER BY created_at DESC, leave_request_id DESC;
            """;

        var requests = new List<LeaveRequest>();

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@status",
            (object?)filter.Status ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@fromDate",
            ToDateParameter(filter.FromDate));

        command.Parameters.AddWithValue(
            "@toDate",
            ToDateParameter(filter.ToDate));

        command.Parameters.AddWithValue(
            "@overdueOnly",
            filter.OverdueOnly);

        command.Parameters.AddWithValue(
            "@today",
            today.ToDateTime(TimeOnly.MinValue));

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            requests.Add(MapRequest(reader));
        }

        return requests;
    }

    public async Task<LeaveStatusTotals> GetStatusTotalsAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        DateOnly today)
    {
        const string query = """
            SELECT
                COUNT(*) AS total_count,
                COALESCE(SUM(status = 'PENDING'), 0)
                    AS pending_count,
                COALESCE(SUM(status = 'APPROVED'), 0)
                    AS approved_count,
                COALESCE(SUM(status = 'REJECTED'), 0)
                    AS rejected_count,
                COALESCE(SUM(status = 'DEPARTED'), 0)
                    AS departed_count,
                COALESCE(SUM(status = 'CLOSED'), 0)
                    AS closed_count,
                COALESCE(SUM(status = 'DEPARTED'
                             AND expected_return_date < @today), 0)
                    AS overdue_count
            FROM leave_requests
            WHERE (@fromDate IS NULL OR departure_date >= @fromDate)
              AND (@toDate IS NULL OR departure_date <= @toDate);
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@fromDate",
            ToDateParameter(fromDate));

        command.Parameters.AddWithValue(
            "@toDate",
            ToDateParameter(toDate));

        command.Parameters.AddWithValue(
            "@today",
            today.ToDateTime(TimeOnly.MinValue));

        await using var reader =
            await command.ExecuteReaderAsync();

        await reader.ReadAsync();

        return new LeaveStatusTotals
        {
            Total = Convert.ToInt32(reader["total_count"]),
            Pending = Convert.ToInt32(reader["pending_count"]),
            Approved = Convert.ToInt32(reader["approved_count"]),
            Rejected = Convert.ToInt32(reader["rejected_count"]),
            Departed = Convert.ToInt32(reader["departed_count"]),
            Closed = Convert.ToInt32(reader["closed_count"]),
            Overdue = Convert.ToInt32(reader["overdue_count"])
        };
    }

    public Task<bool> TryDecideAsync(
        ulong leaveRequestId,
        string newStatus,
        ulong actorUserId,
        string actorRole,
        string reason,
        DateTime occurredAtUtc)
    {
        const string updateQuery = """
            UPDATE leave_requests
            SET
                status = @newStatus,
                decided_by_user_id = @actorUserId,
                decision_reason = @reason,
                decided_at = @occurredAt
            WHERE leave_request_id = @leaveRequestId
              AND status = 'PENDING';
            """;

        string action =
            newStatus == LeaveRequestStatuses.Approved
                ? LeaveAuditActions.Approve
                : LeaveAuditActions.Reject;

        return ApplyTransitionAsync(
            updateQuery,
            command =>
            {
                command.Parameters.AddWithValue(
                    "@newStatus",
                    newStatus);

                command.Parameters.AddWithValue(
                    "@actorUserId",
                    actorUserId);

                command.Parameters.AddWithValue(
                    "@reason",
                    reason);

                command.Parameters.AddWithValue(
                    "@occurredAt",
                    occurredAtUtc);

                command.Parameters.AddWithValue(
                    "@leaveRequestId",
                    leaveRequestId);
            },
            leaveRequestId,
            actorUserId,
            actorRole,
            action,
            LeaveRequestStatuses.Pending,
            newStatus,
            reason,
            null,
            occurredAtUtc);
    }

    public Task<bool> TryRecordDepartureAsync(
        ulong leaveRequestId,
        ulong actorUserId,
        string actorRole,
        DateTime occurredAtUtc)
    {
        const string updateQuery = """
            UPDATE leave_requests
            SET
                status = 'DEPARTED',
                departure_recorded_by_user_id = @actorUserId,
                actual_departure_at = @occurredAt
            WHERE leave_request_id = @leaveRequestId
              AND status = 'APPROVED';
            """;

        return ApplyTransitionAsync(
            updateQuery,
            command =>
            {
                command.Parameters.AddWithValue(
                    "@actorUserId",
                    actorUserId);

                command.Parameters.AddWithValue(
                    "@occurredAt",
                    occurredAtUtc);

                command.Parameters.AddWithValue(
                    "@leaveRequestId",
                    leaveRequestId);
            },
            leaveRequestId,
            actorUserId,
            actorRole,
            LeaveAuditActions.Depart,
            LeaveRequestStatuses.Approved,
            LeaveRequestStatuses.Departed,
            null,
            null,
            occurredAtUtc);
    }

    public Task<bool> TryRecordReturnAsync(
        ulong leaveRequestId,
        ulong actorUserId,
        string actorRole,
        DateTime occurredAtUtc)
    {
        const string updateQuery = """
            UPDATE leave_requests
            SET
                status = 'CLOSED',
                return_recorded_by_user_id = @actorUserId,
                actual_return_at = @occurredAt
            WHERE leave_request_id = @leaveRequestId
              AND status = 'DEPARTED';
            """;

        return ApplyTransitionAsync(
            updateQuery,
            command =>
            {
                command.Parameters.AddWithValue(
                    "@actorUserId",
                    actorUserId);

                command.Parameters.AddWithValue(
                    "@occurredAt",
                    occurredAtUtc);

                command.Parameters.AddWithValue(
                    "@leaveRequestId",
                    leaveRequestId);
            },
            leaveRequestId,
            actorUserId,
            actorRole,
            LeaveAuditActions.Return,
            LeaveRequestStatuses.Departed,
            LeaveRequestStatuses.Closed,
            null,
            null,
            occurredAtUtc);
    }

    public async Task<IReadOnlyList<LeaveRequest>>
        GetOverdueNotAlertedAsync(DateOnly today)
    {
        string query = $"""
            SELECT
                {SelectColumns}
            FROM leave_requests
            WHERE status = 'DEPARTED'
              AND expected_return_date < @today
              AND overdue_alerted_at IS NULL
            ORDER BY expected_return_date, leave_request_id;
            """;

        var requests = new List<LeaveRequest>();

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@today",
            today.ToDateTime(TimeOnly.MinValue));

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            requests.Add(MapRequest(reader));
        }

        return requests;
    }

    public Task<bool> TryFlagOverdueAsync(
        ulong leaveRequestId,
        NewLeaveNotification notification,
        DateTime occurredAtUtc)
    {
        const string updateQuery = """
            UPDATE leave_requests
            SET overdue_alerted_at = @occurredAt
            WHERE leave_request_id = @leaveRequestId
              AND status = 'DEPARTED'
              AND overdue_alerted_at IS NULL;
            """;

        // The system raises this alert, so there is no user.
        return ApplyTransitionAsync(
            updateQuery,
            command =>
            {
                command.Parameters.AddWithValue(
                    "@occurredAt",
                    occurredAtUtc);

                command.Parameters.AddWithValue(
                    "@leaveRequestId",
                    leaveRequestId);
            },
            leaveRequestId,
            null,
            LeaveRoles.System,
            LeaveAuditActions.FlagOverdue,
            LeaveRequestStatuses.Departed,
            LeaveRequestStatuses.Departed,
            null,
            notification,
            occurredAtUtc);
    }

    /// <summary>
    /// Runs a conditional UPDATE and, only if it changed a row,
    /// records the audit row (and optional notification) in the
    /// same transaction. Returns false when no row matched, which
    /// means the request was not in the expected status.
    /// </summary>
    private async Task<bool> ApplyTransitionAsync(
        string updateQuery,
        Action<MySqlCommand> addUpdateParameters,
        ulong leaveRequestId,
        ulong? actorUserId,
        string actorRole,
        string action,
        string fromStatus,
        string toStatus,
        string? remarks,
        NewLeaveNotification? notification,
        DateTime occurredAtUtc)
    {
        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            int changedRows;

            await using (var update = new MySqlCommand(
                updateQuery,
                connection,
                transaction))
            {
                addUpdateParameters(update);

                changedRows = await update.ExecuteNonQueryAsync();
            }

            if (changedRows == 0)
            {
                await transaction.RollbackAsync();
                return false;
            }

            await InsertAuditAsync(
                connection,
                transaction,
                leaveRequestId,
                actorUserId,
                actorRole,
                action,
                fromStatus,
                toStatus,
                remarks,
                occurredAtUtc);

            if (notification is not null)
            {
                await InsertNotificationAsync(
                    connection,
                    transaction,
                    leaveRequestId,
                    notification,
                    occurredAtUtc);
            }

            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static object ToDateParameter(DateOnly? date)
    {
        return date is null
            ? DBNull.Value
            : date.Value.ToDateTime(TimeOnly.MinValue);
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

            DecidedByUserId =
                ReadNullableUInt64(reader, "decided_by_user_id"),

            DecisionReason =
                reader["decision_reason"] is DBNull
                    ? null
                    : Convert.ToString(reader["decision_reason"]),

            DecidedAt =
                ReadNullableUtcDateTime(reader, "decided_at"),

            DepartureRecordedByUserId =
                ReadNullableUInt64(
                    reader,
                    "departure_recorded_by_user_id"),

            ActualDepartureAt =
                ReadNullableUtcDateTime(
                    reader,
                    "actual_departure_at"),

            ReturnRecordedByUserId =
                ReadNullableUInt64(
                    reader,
                    "return_recorded_by_user_id"),

            ActualReturnAt =
                ReadNullableUtcDateTime(
                    reader,
                    "actual_return_at"),

            OverdueAlertedAt =
                ReadNullableUtcDateTime(
                    reader,
                    "overdue_alerted_at"),

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

    private static ulong? ReadNullableUInt64(
        MySqlDataReader reader,
        string column)
    {
        return reader[column] is DBNull
            ? null
            : Convert.ToUInt64(reader[column]);
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
