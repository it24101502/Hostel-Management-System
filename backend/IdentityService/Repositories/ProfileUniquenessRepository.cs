using MySqlConnector;

namespace IdentityService.Repositories;

public class ProfileUniquenessRepository
    : IProfileUniquenessRepository
{
    private readonly string _connectionString;

    public ProfileUniquenessRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task<bool> EmailExistsAsync(
        string normalizedEmail,
        ulong? excludedUserId = null)
    {
        const string query = """
            SELECT COUNT(*)
            FROM users
            WHERE normalized_email = @normalizedEmail
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
            "@normalizedEmail",
            normalizedEmail);

        command.Parameters.AddWithValue(
            "@excludedUserId",
            excludedUserId.HasValue
                ? excludedUserId.Value
                : DBNull.Value);

        object? result =
            await command.ExecuteScalarAsync();

        return Convert.ToInt32(result) > 0;
    }

    public async Task<bool> RegistrationNumberExistsAsync(
        string normalizedRegistrationNumber,
        ulong? excludedStudentProfileId = null)
    {
        const string query = """
            SELECT COUNT(*)
            FROM student_profiles
            WHERE normalized_registration_number =
                  @normalizedRegistrationNumber
              AND
              (
                  @excludedStudentProfileId IS NULL
                  OR student_profile_id <>
                     @excludedStudentProfileId
              );
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@normalizedRegistrationNumber",
            normalizedRegistrationNumber);

        command.Parameters.AddWithValue(
            "@excludedStudentProfileId",
            excludedStudentProfileId.HasValue
                ? excludedStudentProfileId.Value
                : DBNull.Value);

        object? result =
            await command.ExecuteScalarAsync();

        return Convert.ToInt32(result) > 0;
    }
}