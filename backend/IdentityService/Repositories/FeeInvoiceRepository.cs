using IdentityService.DTOs;
using IdentityService.Models;
using MySqlConnector;

namespace IdentityService.Repositories;

public class FeeInvoiceRepository
    : IFeeInvoiceRepository
{
    private readonly string _connectionString;

    public FeeInvoiceRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString(
                "DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task<bool>
        StudentProfileExistsAsync(
            ulong studentProfileId)
    {
        const string query = """
            SELECT COUNT(*)
            FROM student_profiles
            WHERE student_profile_id =
                  @studentProfileId;
            """;

        await using var connection =
            new MySqlConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(
                query,
                connection);

        command.Parameters.AddWithValue(
            "@studentProfileId",
            studentProfileId);

        object? result =
            await command.ExecuteScalarAsync();

        return Convert.ToInt32(result) > 0;
    }

    public async Task<ulong> CreateAsync(
        string invoiceNumber,
        CreateFeeInvoiceRequest request)
    {
        const string query = """
            INSERT INTO fee_invoices
            (
                student_profile_id,
                invoice_number,
                fee_type,
                description,
                total_amount,
                paid_amount,
                issued_at,
                due_date,
                status
            )
            VALUES
            (
                @studentProfileId,
                @invoiceNumber,
                @feeType,
                @description,
                @totalAmount,
                0.00,
                UTC_TIMESTAMP(),
                @dueDate,
                'UNPAID'
            );
            """;

        await using var connection =
            new MySqlConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(
                query,
                connection);

        command.Parameters.AddWithValue(
            "@studentProfileId",
            request.StudentProfileId!.Value);

        command.Parameters.AddWithValue(
            "@invoiceNumber",
            invoiceNumber);

        command.Parameters.AddWithValue(
            "@feeType",
            request.FeeType);

        command.Parameters.AddWithValue(
            "@description",
            string.IsNullOrWhiteSpace(
                request.Description)
                ? DBNull.Value
                : request.Description.Trim());

        command.Parameters.AddWithValue(
            "@totalAmount",
            request.Amount!.Value);

        command.Parameters.AddWithValue(
            "@dueDate",
            request.DueDate!.Value.ToDateTime(
                TimeOnly.MinValue));

        await command.ExecuteNonQueryAsync();

        return (ulong)command.LastInsertedId;
    }

    public async Task<FeeInvoice?> GetByIdAsync(
        ulong invoiceId)
    {
        const string query = """
            SELECT
                invoice_id,
                student_profile_id,
                invoice_number,
                fee_type,
                description,
                total_amount,
                paid_amount,
                issued_at,
                due_date,
                status,
                created_at,
                updated_at
            FROM fee_invoices
            WHERE invoice_id = @invoiceId
            LIMIT 1;
            """;

        await using var connection =
            new MySqlConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(
                query,
                connection);

        command.Parameters.AddWithValue(
            "@invoiceId",
            invoiceId);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapInvoice(reader);
    }

    private static FeeInvoice MapInvoice(
        MySqlDataReader reader)
    {
        return new FeeInvoice
        {
            InvoiceId =
                reader.GetUInt64(
                    "invoice_id"),

            StudentProfileId =
                reader.GetUInt64(
                    "student_profile_id"),

            InvoiceNumber =
                reader.GetString(
                    "invoice_number"),

            FeeType =
                reader.GetString(
                    "fee_type"),

            Description =
                reader.IsDBNull(
                    reader.GetOrdinal(
                        "description"))
                    ? null
                    : reader.GetString(
                        "description"),

            TotalAmount =
                reader.GetDecimal(
                    "total_amount"),

            PaidAmount =
                reader.GetDecimal(
                    "paid_amount"),

            IssuedAt =
                reader.GetDateTime(
                    "issued_at"),

            DueDate =
                DateOnly.FromDateTime(
                    reader.GetDateTime(
                        "due_date")),

            Status =
                reader.GetString(
                    "status"),

            CreatedAt =
                reader.GetDateTime(
                    "created_at"),

            UpdatedAt =
                reader.GetDateTime(
                    "updated_at")
        };
    }
}