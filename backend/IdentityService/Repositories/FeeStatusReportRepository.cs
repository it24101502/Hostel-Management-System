using IdentityService.DTOs;
using MySqlConnector;

namespace IdentityService.Repositories;

public class FeeStatusReportRepository
    : IFeeStatusReportRepository
{
    private readonly string _connectionString;

    public FeeStatusReportRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString(
                "DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task<IReadOnlyList<FeeStatusReportRow>>
        GetReportAsync(
            ulong? studentProfileId,
            ulong? blockId)
    {
        const string query = """
            SELECT
                fi.invoice_id,
                fi.invoice_number,
                sp.student_profile_id,
                sp.registration_number,

                CONCAT(
                    u.first_name,
                    ' ',
                    u.last_name
                ) AS student_name,

                u.email,
                hb.block_id,
                hb.block_code,
                hb.block_name,
                fi.fee_type,
                fi.total_amount,
                fi.paid_amount,

                GREATEST(
                    fi.total_amount - fi.paid_amount,
                    0
                ) AS outstanding_amount,

                fi.issued_at,
                fi.due_date,

                CASE
                    WHEN fi.status = 'CANCELLED'
                        THEN 'CANCELLED'

                    WHEN fi.paid_amount >=
                         fi.total_amount
                        THEN 'PAID'

                    WHEN fi.due_date < UTC_DATE()
                        THEN 'OVERDUE'

                    ELSE 'UNPAID'
                END AS current_status

            FROM fee_invoices AS fi

            INNER JOIN student_profiles AS sp
                ON sp.student_profile_id =
                   fi.student_profile_id

            INNER JOIN users AS u
                ON u.user_id = sp.user_id

            LEFT JOIN hostel_blocks AS hb
                ON hb.block_id =
                   sp.hostel_block_id

            WHERE
                (
                    @studentProfileId IS NULL
                    OR sp.student_profile_id =
                       @studentProfileId
                )
                AND
                (
                    @blockId IS NULL
                    OR sp.hostel_block_id =
                       @blockId
                )

            ORDER BY
                sp.registration_number,
                fi.due_date DESC,
                fi.invoice_id DESC;
            """;

        var rows =
            new List<FeeStatusReportRow>();

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
            studentProfileId.HasValue
                ? studentProfileId.Value
                : DBNull.Value);

        command.Parameters.AddWithValue(
            "@blockId",
            blockId.HasValue
                ? blockId.Value
                : DBNull.Value);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            rows.Add(new FeeStatusReportRow
            {
                InvoiceId =
                    reader.GetUInt64("invoice_id"),

                InvoiceNumber =
                    reader.GetString("invoice_number"),

                StudentProfileId =
                    reader.GetUInt64(
                        "student_profile_id"),

                RegistrationNumber =
                    reader.GetString(
                        "registration_number"),

                StudentName =
                    reader.GetString("student_name"),

                Email =
                    reader.GetString("email"),

                BlockId =
                    reader.IsDBNull(
                        reader.GetOrdinal("block_id"))
                        ? null
                        : reader.GetUInt64("block_id"),

                BlockCode =
                    reader.IsDBNull(
                        reader.GetOrdinal("block_code"))
                        ? null
                        : reader.GetString("block_code"),

                BlockName =
                    reader.IsDBNull(
                        reader.GetOrdinal("block_name"))
                        ? null
                        : reader.GetString("block_name"),

                FeeType =
                    reader.GetString("fee_type"),

                TotalAmount =
                    reader.GetDecimal("total_amount"),

                PaidAmount =
                    reader.GetDecimal("paid_amount"),

                OutstandingAmount =
                    reader.GetDecimal(
                        "outstanding_amount"),

                IssuedAt =
                    reader.GetDateTime("issued_at"),

                DueDate =
                    DateOnly.FromDateTime(
                        reader.GetDateTime("due_date")),

                Status =
                    reader.GetString("current_status")
            });
        }

        return rows;
    }
}