using IdentityService.Models;
using MySqlConnector;

namespace IdentityService.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task<LoginUser?> FindByIdentifierAsync(
        string identifier)
    {
        const string query = """
            SELECT
                u.user_id,
                u.username,
                u.email,
                u.password_hash,
                u.failed_login_attempts,
                u.is_locked,
                u.lockout_end_at,
                u.is_active,
                r.role_name,
                r.is_active AS role_is_active
            FROM users AS u
            INNER JOIN roles AS r
                ON r.role_id = u.role_id
            WHERE u.normalized_email = @identifier
               OR u.normalized_username = @identifier
            LIMIT 1;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@identifier",
            identifier);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        int lockoutOrdinal =
            reader.GetOrdinal("lockout_end_at");

        return new LoginUser
        {
            UserId = reader.GetUInt64("user_id"),
            Username = reader.GetString("username"),
            Email = reader.GetString("email"),
            PasswordHash =
                reader.GetString("password_hash"),

            FailedLoginAttempts =
                reader.GetUInt32("failed_login_attempts"),

            IsLocked =
                reader.GetBoolean("is_locked"),

            LockoutEndAt =
                reader.IsDBNull(lockoutOrdinal)
                    ? null
                    : reader.GetDateTime(lockoutOrdinal),

            IsActive =
                reader.GetBoolean("is_active"),

            RoleName =
                reader.GetString("role_name"),

            IsRoleActive =
                reader.GetBoolean("role_is_active")
        };
    }

    public async Task RecordSuccessfulLoginAsync(
        ulong userId)
    {
        const string query = """
            UPDATE users
            SET failed_login_attempts = 0,
                is_locked = FALSE,
                lockout_end_at = NULL,
                last_login_at = UTC_TIMESTAMP()
            WHERE user_id = @userId;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@userId",
            userId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task RecordFailedLoginAsync(
        ulong userId,
        uint failedAttempts,
        bool isLocked,
        DateTime? lockoutEndAt)
    {
        const string query = """
            UPDATE users
            SET failed_login_attempts = @failedAttempts,
                is_locked = @isLocked,
                lockout_end_at = @lockoutEndAt
            WHERE user_id = @userId;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@failedAttempts",
            failedAttempts);

        command.Parameters.AddWithValue(
            "@isLocked",
            isLocked);

        command.Parameters.AddWithValue(
            "@lockoutEndAt",
            (object?)lockoutEndAt ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@userId",
            userId);

        await command.ExecuteNonQueryAsync();
    }
}