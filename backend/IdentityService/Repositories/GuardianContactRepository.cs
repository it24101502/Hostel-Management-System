using IdentityService.DTOs;
using IdentityService.Models;
using MySqlConnector;

namespace IdentityService.Repositories;

public class GuardianContactRepository : IGuardianContactRepository
{
    private readonly string _connectionString;

    public GuardianContactRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task<bool> StudentProfileExistsAsync(
        ulong studentProfileId)
    {
        const string query = """
            SELECT COUNT(*)
            FROM student_profiles
            WHERE student_profile_id = @studentProfileId;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@studentProfileId",
            studentProfileId);

        object? result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result) > 0;
    }

    public async Task<IReadOnlyList<GuardianContact>>
        GetByStudentProfileIdAsync(ulong studentProfileId)
    {
        const string query = """
            SELECT
                guardian_contact_id,
                student_profile_id,
                contact_type,
                full_name,
                relationship,
                phone_number,
                alternate_phone,
                email,
                address,
                is_primary,
                is_active,
                created_at,
                updated_at
            FROM guardian_contacts
            WHERE student_profile_id = @studentProfileId
            ORDER BY is_primary DESC, guardian_contact_id;
            """;

        var contacts = new List<GuardianContact>();

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

        while (await reader.ReadAsync())
        {
            contacts.Add(MapContact(reader));
        }

        return contacts;
    }

    public async Task<GuardianContact?> GetByIdAsync(
        ulong studentProfileId,
        ulong contactId)
    {
        const string query = """
            SELECT
                guardian_contact_id,
                student_profile_id,
                contact_type,
                full_name,
                relationship,
                phone_number,
                alternate_phone,
                email,
                address,
                is_primary,
                is_active,
                created_at,
                updated_at
            FROM guardian_contacts
            WHERE guardian_contact_id = @contactId
              AND student_profile_id = @studentProfileId
            LIMIT 1;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@contactId",
            contactId);

        command.Parameters.AddWithValue(
            "@studentProfileId",
            studentProfileId);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapContact(reader);
    }

    public async Task<ulong> CreateAsync(
        ulong studentProfileId,
        CreateGuardianContactRequest request)
    {
        const string query = """
            INSERT INTO guardian_contacts
            (
                student_profile_id,
                contact_type,
                full_name,
                relationship,
                phone_number,
                alternate_phone,
                email,
                address,
                is_primary,
                is_active
            )
            VALUES
            (
                @studentProfileId,
                @contactType,
                @fullName,
                @relationship,
                @phoneNumber,
                @alternatePhone,
                @email,
                @address,
                @isPrimary,
                TRUE
            );
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        AddContactParameters(
            command,
            studentProfileId,
            request.ContactType,
            request.FullName,
            request.Relationship,
            request.PhoneNumber,
            request.AlternatePhone,
            request.Email,
            request.Address,
            request.IsPrimary);

        await command.ExecuteNonQueryAsync();

        return (ulong)command.LastInsertedId;
    }

    public async Task<bool> UpdateAsync(
        ulong studentProfileId,
        ulong contactId,
        UpdateGuardianContactRequest request)
    {
        const string query = """
            UPDATE guardian_contacts
            SET
                contact_type = @contactType,
                full_name = @fullName,
                relationship = @relationship,
                phone_number = @phoneNumber,
                alternate_phone = @alternatePhone,
                email = @email,
                address = @address,
                is_primary = @isPrimary,
                is_active = @isActive
            WHERE guardian_contact_id = @contactId
              AND student_profile_id = @studentProfileId;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        AddContactParameters(
            command,
            studentProfileId,
            request.ContactType,
            request.FullName,
            request.Relationship,
            request.PhoneNumber,
            request.AlternatePhone,
            request.Email,
            request.Address,
            request.IsPrimary);

        command.Parameters.AddWithValue(
            "@contactId",
            contactId);

        command.Parameters.AddWithValue(
            "@isActive",
            request.IsActive);

        int affectedRows = await command.ExecuteNonQueryAsync();

        return affectedRows > 0;
    }

    private static void AddContactParameters(
        MySqlCommand command,
        ulong studentProfileId,
        string contactType,
        string fullName,
        string relationship,
        string phoneNumber,
        string? alternatePhone,
        string? email,
        string? address,
        bool isPrimary)
    {
        command.Parameters.AddWithValue(
            "@studentProfileId",
            studentProfileId);

        command.Parameters.AddWithValue(
            "@contactType",
            contactType.Trim().ToUpperInvariant());

        command.Parameters.AddWithValue(
            "@fullName",
            fullName.Trim());

        command.Parameters.AddWithValue(
            "@relationship",
            relationship.Trim());

        command.Parameters.AddWithValue(
            "@phoneNumber",
            phoneNumber.Trim());

        command.Parameters.AddWithValue(
            "@alternatePhone",
            string.IsNullOrWhiteSpace(alternatePhone)
                ? DBNull.Value
                : alternatePhone.Trim());

        command.Parameters.AddWithValue(
            "@email",
            string.IsNullOrWhiteSpace(email)
                ? DBNull.Value
                : email.Trim());

        command.Parameters.AddWithValue(
            "@address",
            string.IsNullOrWhiteSpace(address)
                ? DBNull.Value
                : address.Trim());

        command.Parameters.AddWithValue(
            "@isPrimary",
            isPrimary);
    }

    private static GuardianContact MapContact(
        MySqlDataReader reader)
    {
        return new GuardianContact
        {
            GuardianContactId =
                reader.GetUInt64("guardian_contact_id"),

            StudentProfileId =
                reader.GetUInt64("student_profile_id"),

            ContactType =
                reader.GetString("contact_type"),

            FullName =
                reader.GetString("full_name"),

            Relationship =
                reader.GetString("relationship"),

            PhoneNumber =
                reader.GetString("phone_number"),

            AlternatePhone =
                reader.IsDBNull(
                    reader.GetOrdinal("alternate_phone"))
                    ? null
                    : reader.GetString("alternate_phone"),

            Email =
                reader.IsDBNull(reader.GetOrdinal("email"))
                    ? null
                    : reader.GetString("email"),

            Address =
                reader.IsDBNull(reader.GetOrdinal("address"))
                    ? null
                    : reader.GetString("address"),

            IsPrimary =
                reader.GetBoolean("is_primary"),

            IsActive =
                reader.GetBoolean("is_active"),

            CreatedAt =
                reader.GetDateTime("created_at"),

            UpdatedAt =
                reader.GetDateTime("updated_at")
        };
    }
}