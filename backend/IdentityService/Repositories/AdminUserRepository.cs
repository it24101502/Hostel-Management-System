using IdentityService.DTOs;
using IdentityService.Models;
using MySqlConnector;

namespace IdentityService.Repositories;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly string _connectionString;

    public AdminUserRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task<IReadOnlyList<UserAccount>> GetAllAsync()
    {
        const string query = """
            SELECT
                u.user_id,
                u.role_id,
                r.role_name,
                u.username,
                u.email,
                u.first_name,
                u.last_name,
                u.phone_number,
                u.is_active,
                u.created_at,
                u.updated_at
            FROM users AS u
            INNER JOIN roles AS r
                ON r.role_id = u.role_id
            ORDER BY u.user_id;
            """;

        var users = new List<UserAccount>();

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            users.Add(MapUser(reader));
        }

        return users;
    }

    public async Task<UserAccount?> GetByIdAsync(ulong userId)
    {
        const string query = """
            SELECT
                u.user_id,
                u.role_id,
                r.role_name,
                u.username,
                u.email,
                u.first_name,
                u.last_name,
                u.phone_number,
                u.is_active,
                u.created_at,
                u.updated_at
            FROM users AS u
            INNER JOIN roles AS r
                ON r.role_id = u.role_id
            WHERE u.user_id = @userId
            LIMIT 1;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue("@userId", userId);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapUser(reader);
    }

    public async Task<bool> RoleExistsAsync(ulong roleId)
    {
        const string query = """
            SELECT COUNT(*)
            FROM roles
            WHERE role_id = @roleId
              AND is_active = TRUE;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue("@roleId", roleId);

        object? result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result) > 0;
    }

    public async Task<bool> UsernameOrEmailExistsAsync(
        string normalizedUsername,
        string normalizedEmail,
        ulong? excludedUserId = null)
    {
        const string query = """
            SELECT COUNT(*)
            FROM users
            WHERE
                (
                    normalized_username = @normalizedUsername
                    OR normalized_email = @normalizedEmail
                )
                AND
                (
                    @excludedUserId IS NULL
                    OR user_id <> @excludedUserId
                );
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@normalizedUsername",
            normalizedUsername);

        command.Parameters.AddWithValue(
            "@normalizedEmail",
            normalizedEmail);

        command.Parameters.AddWithValue(
            "@excludedUserId",
            excludedUserId.HasValue
                ? excludedUserId.Value
                : DBNull.Value);

        object? result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result) > 0;
    }

    public async Task<ulong> CreateAsync(
        CreateUserRequest request,
        string passwordHash)
    {
        const string query = """
            INSERT INTO users
            (
                role_id,
                username,
                normalized_username,
                email,
                normalized_email,
                password_hash,
                first_name,
                last_name,
                phone_number,
                failed_login_attempts,
                is_locked,
                is_active,
                is_email_verified
            )
            VALUES
            (
                @roleId,
                @username,
                @normalizedUsername,
                @email,
                @normalizedEmail,
                @passwordHash,
                @firstName,
                @lastName,
                @phoneNumber,
                0,
                FALSE,
                TRUE,
                FALSE
            );
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@roleId",
            request.RoleId);

        command.Parameters.AddWithValue(
            "@username",
            request.Username.Trim());

        command.Parameters.AddWithValue(
            "@normalizedUsername",
            request.Username.Trim().ToUpperInvariant());

        command.Parameters.AddWithValue(
            "@email",
            request.Email.Trim());

        command.Parameters.AddWithValue(
            "@normalizedEmail",
            request.Email.Trim().ToUpperInvariant());

        command.Parameters.AddWithValue(
            "@passwordHash",
            passwordHash);

        command.Parameters.AddWithValue(
            "@firstName",
            request.FirstName.Trim());

        command.Parameters.AddWithValue(
            "@lastName",
            request.LastName.Trim());

        command.Parameters.AddWithValue(
            "@phoneNumber",
            string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? DBNull.Value
                : request.PhoneNumber.Trim());

        await command.ExecuteNonQueryAsync();

        return (ulong)command.LastInsertedId;
    }

    public async Task<bool> UpdateAsync(
        ulong userId,
        UpdateUserRequest request)
    {
        const string query = """
            UPDATE users
            SET
                role_id = @roleId,
                username = @username,
                normalized_username = @normalizedUsername,
                email = @email,
                normalized_email = @normalizedEmail,
                first_name = @firstName,
                last_name = @lastName,
                phone_number = @phoneNumber
            WHERE user_id = @userId;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@roleId",
            request.RoleId);

        command.Parameters.AddWithValue(
            "@username",
            request.Username.Trim());

        command.Parameters.AddWithValue(
            "@normalizedUsername",
            request.Username.Trim().ToUpperInvariant());

        command.Parameters.AddWithValue(
            "@email",
            request.Email.Trim());

        command.Parameters.AddWithValue(
            "@normalizedEmail",
            request.Email.Trim().ToUpperInvariant());

        command.Parameters.AddWithValue(
            "@firstName",
            request.FirstName.Trim());

        command.Parameters.AddWithValue(
            "@lastName",
            request.LastName.Trim());

        command.Parameters.AddWithValue(
            "@phoneNumber",
            string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? DBNull.Value
                : request.PhoneNumber.Trim());

        command.Parameters.AddWithValue("@userId", userId);

        int affectedRows = await command.ExecuteNonQueryAsync();

        return affectedRows > 0;
    }

    public async Task<bool> DeactivateAsync(ulong userId)
    {
        const string query = """
            UPDATE users
            SET
                is_active = FALSE,
                deleted_at = UTC_TIMESTAMP()
            WHERE user_id = @userId;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue("@userId", userId);

        int affectedRows = await command.ExecuteNonQueryAsync();

        return affectedRows > 0;
    }

    private static UserAccount MapUser(
        MySqlDataReader reader)
    {
        int phoneNumberOrdinal =
            reader.GetOrdinal("phone_number");

        return new UserAccount
        {
            UserId = reader.GetUInt64("user_id"),
            RoleId = reader.GetUInt64("role_id"),
            RoleName = reader.GetString("role_name"),
            Username = reader.GetString("username"),
            Email = reader.GetString("email"),
            FirstName = reader.GetString("first_name"),
            LastName = reader.GetString("last_name"),

            PhoneNumber =
                reader.IsDBNull(phoneNumberOrdinal)
                    ? null
                    : reader.GetString(phoneNumberOrdinal),

            IsActive = reader.GetBoolean("is_active"),
            CreatedAt = reader.GetDateTime("created_at"),
            UpdatedAt = reader.GetDateTime("updated_at")
        };
    }
}