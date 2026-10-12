using IdentityService.DTOs;
using MySqlConnector;

namespace IdentityService.Repositories;

public class StudentFeeReminderRepository
    : IStudentFeeReminderRepository
{
    private readonly string _connectionString;

    public StudentFeeReminderRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task<IReadOnlyList<FeeReminderResponse>>
        GetForUserAsync(ulong userId)
    {
        // Only reminders that were actually sent, and only while
        // the invoice is still unpaid and not cancelled.
        const string query = """
            SELECT
                fr.reminder_id,
                fr.invoice_id,
                fi.invoice_number,
                fi.fee_type,
                fi.total_amount,
                fi.paid_amount,
                fi.due_date,
                fr.message,
                fr.triggered_at,
                fr.sent_at
            FROM fee_reminder_notifications AS fr
            INNER JOIN fee_invoices AS fi
                ON fi.invoice_id = fr.invoice_id
            WHERE fr.recipient_user_id = @userId
              AND fr.notification_status = 'SENT'
              AND fi.status <> 'CANCELLED'
              AND fi.paid_amount < fi.total_amount
            ORDER BY fr.triggered_at DESC, fr.reminder_id DESC;
            """;

        var reminders = new List<FeeReminderResponse>();

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue("@userId", userId);

        await using var reader =
            await command.ExecuteReaderAsync();

        int sentAtOrdinal = reader.GetOrdinal("sent_at");

        while (await reader.ReadAsync())
        {
            decimal total = reader.GetDecimal("total_amount");
            decimal paid = reader.GetDecimal("paid_amount");

            reminders.Add(new FeeReminderResponse
            {
                ReminderId = reader.GetUInt64("reminder_id"),
                InvoiceId = reader.GetUInt64("invoice_id"),
                InvoiceNumber = reader.GetString("invoice_number"),
                FeeType = reader.GetString("fee_type"),
                TotalAmount = total,
                PaidAmount = paid,
                OutstandingAmount = Math.Max(total - paid, 0m),
                DueDate = DateOnly.FromDateTime(
                    reader.GetDateTime("due_date")),
                Message = reader.GetString("message"),

                // MySQL DATETIME has no time zone; the job stores UTC.
                TriggeredAt = DateTime.SpecifyKind(
                    reader.GetDateTime("triggered_at"),
                    DateTimeKind.Utc),

                SentAt = reader.IsDBNull(sentAtOrdinal)
                    ? null
                    : DateTime.SpecifyKind(
                        reader.GetDateTime(sentAtOrdinal),
                        DateTimeKind.Utc)
            });
        }

        return reminders;
    }
}