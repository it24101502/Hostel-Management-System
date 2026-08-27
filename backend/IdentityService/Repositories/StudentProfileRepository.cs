using IdentityService.DTOs;
using IdentityService.Models;
using MySqlConnector;

namespace IdentityService.Repositories;

public class StudentProfileRepository
    : IStudentProfileRepository
{
    private readonly string _connectionString;

    public StudentProfileRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task<bool> UserExistsAsync(ulong userId)
    {
        const string query = """
            SELECT COUNT(*)
            FROM users
            WHERE user_id = @userId
              AND is_active = TRUE;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@userId",
            userId);

        object? result =
            await command.ExecuteScalarAsync();

        return Convert.ToInt32(result) > 0;
    }

    public async Task<bool> ProfileExistsForUserAsync(
        ulong userId)
    {
        const string query = """
            SELECT COUNT(*)
            FROM student_profiles
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

        object? result =
            await command.ExecuteScalarAsync();

        return Convert.ToInt32(result) > 0;
    }

    public async Task<StudentProfile?> GetByIdAsync(
        ulong studentProfileId)
    {
        const string query = """
            SELECT
                sp.student_profile_id,
                sp.user_id,
                u.email,
                sp.registration_number,
                sp.date_of_birth,
                sp.gender,
                sp.address_line_1,
                sp.address_line_2,
                sp.city,
                sp.district,
                sp.postal_code,
                sp.programme_name,
                sp.faculty_name,
                sp.academic_year,
                sp.profile_photo_url,
                sp.created_at,
                sp.updated_at
            FROM student_profiles AS sp
            INNER JOIN users AS u
                ON u.user_id = sp.user_id
            WHERE sp.student_profile_id =
                  @studentProfileId
            LIMIT 1;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@studentProfileId",
            studentProfileId);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapProfile(reader);
    }

    public async Task<ulong> CreateAsync(
        CreateStudentProfileRequest request)
    {
        const string updateEmailQuery = """
            UPDATE users
            SET
                email = @email,
                normalized_email = @normalizedEmail
            WHERE user_id = @userId;
            """;

        const string insertProfileQuery = """
            INSERT INTO student_profiles
            (
                user_id,
                registration_number,
                normalized_registration_number,
                date_of_birth,
                gender,
                address_line_1,
                address_line_2,
                city,
                district,
                postal_code,
                programme_name,
                faculty_name,
                academic_year,
                profile_photo_url
            )
            VALUES
            (
                @userId,
                @registrationNumber,
                @normalizedRegistrationNumber,
                @dateOfBirth,
                @gender,
                @addressLine1,
                @addressLine2,
                @city,
                @district,
                @postalCode,
                @programmeName,
                @facultyName,
                @academicYear,
                @profilePhotoUrl
            );
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            await using (
                var emailCommand =
                    new MySqlCommand(
                        updateEmailQuery,
                        connection,
                        transaction))
            {
                emailCommand.Parameters.AddWithValue(
                    "@userId",
                    request.UserId);

                emailCommand.Parameters.AddWithValue(
                    "@email",
                    request.Email.Trim());

                emailCommand.Parameters.AddWithValue(
                    "@normalizedEmail",
                    request.Email
                        .Trim()
                        .ToUpperInvariant());

                await emailCommand.ExecuteNonQueryAsync();
            }

            ulong profileId;

            await using (
                var profileCommand =
                    new MySqlCommand(
                        insertProfileQuery,
                        connection,
                        transaction))
            {
                AddProfileParameters(
                    profileCommand,
                    request.UserId,
                    request.Email,
                    request.RegistrationNumber,
                    request.DateOfBirth,
                    request.Gender,
                    request.AddressLine1,
                    request.AddressLine2,
                    request.City,
                    request.District,
                    request.PostalCode,
                    request.ProgrammeName,
                    request.FacultyName,
                    request.AcademicYear,
                    request.ProfilePhotoUrl);

                await profileCommand.ExecuteNonQueryAsync();

                profileId =
                    (ulong)profileCommand.LastInsertedId;
            }

            await transaction.CommitAsync();

            return profileId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(
        ulong studentProfileId,
        UpdateStudentProfileRequest request)
    {
        const string updateEmailQuery = """
            UPDATE users AS u
            INNER JOIN student_profiles AS sp
                ON sp.user_id = u.user_id
            SET
                u.email = @email,
                u.normalized_email = @normalizedEmail
            WHERE sp.student_profile_id =
                  @studentProfileId;
            """;

        const string updateProfileQuery = """
            UPDATE student_profiles
            SET
                registration_number =
                    @registrationNumber,
                normalized_registration_number =
                    @normalizedRegistrationNumber,
                date_of_birth = @dateOfBirth,
                gender = @gender,
                address_line_1 = @addressLine1,
                address_line_2 = @addressLine2,
                city = @city,
                district = @district,
                postal_code = @postalCode,
                programme_name = @programmeName,
                faculty_name = @facultyName,
                academic_year = @academicYear,
                profile_photo_url = @profilePhotoUrl
            WHERE student_profile_id =
                  @studentProfileId;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            await using (
                var emailCommand =
                    new MySqlCommand(
                        updateEmailQuery,
                        connection,
                        transaction))
            {
                emailCommand.Parameters.AddWithValue(
                    "@studentProfileId",
                    studentProfileId);

                emailCommand.Parameters.AddWithValue(
                    "@email",
                    request.Email.Trim());

                emailCommand.Parameters.AddWithValue(
                    "@normalizedEmail",
                    request.Email
                        .Trim()
                        .ToUpperInvariant());

                await emailCommand.ExecuteNonQueryAsync();
            }

            int affectedRows;

            await using (
                var profileCommand =
                    new MySqlCommand(
                        updateProfileQuery,
                        connection,
                        transaction))
            {
                AddProfileParameters(
                    profileCommand,
                    userId: 0,
                    request.Email,
                    request.RegistrationNumber,
                    request.DateOfBirth,
                    request.Gender,
                    request.AddressLine1,
                    request.AddressLine2,
                    request.City,
                    request.District,
                    request.PostalCode,
                    request.ProgrammeName,
                    request.FacultyName,
                    request.AcademicYear,
                    request.ProfilePhotoUrl);

                profileCommand.Parameters.AddWithValue(
                    "@studentProfileId",
                    studentProfileId);

                affectedRows =
                    await profileCommand
                        .ExecuteNonQueryAsync();
            }

            if (affectedRows == 0)
            {
                await transaction.RollbackAsync();
                return false;
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

    private static void AddProfileParameters(
        MySqlCommand command,
        ulong userId,
        string email,
        string registrationNumber,
        DateTime? dateOfBirth,
        string? gender,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? district,
        string? postalCode,
        string? programmeName,
        string? facultyName,
        uint? academicYear,
        string? profilePhotoUrl)
    {
        command.Parameters.AddWithValue(
            "@userId",
            userId);

        command.Parameters.AddWithValue(
            "@registrationNumber",
            registrationNumber.Trim());

        command.Parameters.AddWithValue(
            "@normalizedRegistrationNumber",
            registrationNumber
                .Trim()
                .ToUpperInvariant());

        command.Parameters.AddWithValue(
            "@dateOfBirth",
            dateOfBirth.HasValue
                ? dateOfBirth.Value
                : DBNull.Value);

        AddNullableString(
            command,
            "@gender",
            gender?.ToUpperInvariant());

        AddNullableString(
            command,
            "@addressLine1",
            addressLine1);

        AddNullableString(
            command,
            "@addressLine2",
            addressLine2);

        AddNullableString(command, "@city", city);

        AddNullableString(
            command,
            "@district",
            district);

        AddNullableString(
            command,
            "@postalCode",
            postalCode);

        AddNullableString(
            command,
            "@programmeName",
            programmeName);

        AddNullableString(
            command,
            "@facultyName",
            facultyName);

        command.Parameters.AddWithValue(
            "@academicYear",
            academicYear.HasValue
                ? academicYear.Value
                : DBNull.Value);

        AddNullableString(
            command,
            "@profilePhotoUrl",
            profilePhotoUrl);
    }

    private static void AddNullableString(
        MySqlCommand command,
        string parameterName,
        string? value)
    {
        command.Parameters.AddWithValue(
            parameterName,
            string.IsNullOrWhiteSpace(value)
                ? DBNull.Value
                : value.Trim());
    }

    private static StudentProfile MapProfile(
        MySqlDataReader reader)
    {
        return new StudentProfile
        {
            StudentProfileId =
                reader.GetUInt64("student_profile_id"),

            UserId = reader.GetUInt64("user_id"),

            Email = reader.GetString("email"),

            RegistrationNumber =
                reader.GetString("registration_number"),

            DateOfBirth =
                GetNullableDateTime(
                    reader,
                    "date_of_birth"),

            Gender =
                GetNullableString(reader, "gender"),

            AddressLine1 =
                GetNullableString(
                    reader,
                    "address_line_1"),

            AddressLine2 =
                GetNullableString(
                    reader,
                    "address_line_2"),

            City = GetNullableString(reader, "city"),

            District =
                GetNullableString(reader, "district"),

            PostalCode =
                GetNullableString(
                    reader,
                    "postal_code"),

            ProgrammeName =
                GetNullableString(
                    reader,
                    "programme_name"),

            FacultyName =
                GetNullableString(
                    reader,
                    "faculty_name"),

            AcademicYear =
                GetNullableUInt32(
                    reader,
                    "academic_year"),

            ProfilePhotoUrl =
                GetNullableString(
                    reader,
                    "profile_photo_url"),

            CreatedAt =
                reader.GetDateTime("created_at"),

            UpdatedAt =
                reader.GetDateTime("updated_at")
        };
    }

    private static string? GetNullableString(
        MySqlDataReader reader,
        string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
    }

    private static DateTime? GetNullableDateTime(
        MySqlDataReader reader,
        string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetDateTime(ordinal);
    }

    private static uint? GetNullableUInt32(
        MySqlDataReader reader,
        string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetUInt32(ordinal);
    }
}