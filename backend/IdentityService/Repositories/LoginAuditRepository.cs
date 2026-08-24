using IdentityService.Models;
using MySqlConnector;

namespace IdentityService.Repositories;

public sealed class LoginAuditRepository
    : ILoginAuditRepository
{
    private readonly string _connectionString;

    public LoginAuditRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString(
                "DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task RecordAttemptAsync(
        ulong? userId,
        string identifier,
        string outcome)
    {
        const string query = """
            INSERT INTO login_audit_logs
            (
                user_id,
                identifier,
                outcome,
                attempted_at
            )
            VALUES
            (
                @userId,
                @identifier,
                @outcome,
                UTC_TIMESTAMP(6)
            );
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@userId",
            userId.HasValue
                ? userId.Value
                : DBNull.Value);

        command.Parameters.AddWithValue(
            "@identifier",
            identifier);

        command.Parameters.AddWithValue(
            "@outcome",
            outcome);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<LoginAuditLog>>
        GetRecentAttemptsAsync(int limit)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                "Limit must be greater than zero.");
        }

        const string query = """
            SELECT
                audit_log_id,
                user_id,
                identifier,
                outcome,
                attempted_at
            FROM login_audit_logs
            ORDER BY attempted_at DESC,
                     audit_log_id DESC
            LIMIT @limit;
            """;

        var logs = new List<LoginAuditLog>();

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@limit",
            limit);

        await using var reader =
            await command.ExecuteReaderAsync();

        int userIdOrdinal =
            reader.GetOrdinal("user_id");

        while (await reader.ReadAsync())
        {
            logs.Add(new LoginAuditLog
            {
                AuditLogId =
                    reader.GetUInt64("audit_log_id"),

                UserId =
                    reader.IsDBNull(userIdOrdinal)
                        ? null
                        : reader.GetUInt64(userIdOrdinal),

                Identifier =
                    reader.GetString("identifier"),

                Outcome =
                    reader.GetString("outcome"),

                AttemptedAt =
                    reader.GetDateTime("attempted_at")
            });
        }

        return logs;
    }
}