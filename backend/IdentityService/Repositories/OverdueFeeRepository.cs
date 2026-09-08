using IdentityService.Models;
using MySqlConnector;

namespace IdentityService.Repositories;

public class OverdueFeeRepository
    : IOverdueFeeRepository
{
    private readonly string _connectionString;

    public OverdueFeeRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString(
                "DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task<int>
        MarkOverdueAndCreateRemindersAsync(
            DateOnly processingDate,
            CancellationToken cancellationToken =
                default)
    {
        await using var connection =
            new MySqlConnection(
                _connectionString);

        await connection.OpenAsync(
            cancellationToken);

        await using var transaction =
            await connection.BeginTransactionAsync(
                cancellationToken);

        try
        {
            IReadOnlyList<OverdueCandidate>
                candidates =
                    await GetOverdueCandidatesAsync(
                        connection,
                        transaction,
                        processingDate,
                        cancellationToken);

            int markedCount = 0;

            foreach (
                OverdueCandidate candidate
                in candidates)
            {
                int affectedRows =
                    await MarkInvoiceOverdueAsync(
                        connection,
                        transaction,
                        candidate.InvoiceId,
                        cancellationToken);

                if (affectedRows == 0)
                {
                    continue;
                }

                markedCount += affectedRows;

                await CreateReminderAsync(
                    connection,
                    transaction,
                    candidate,
                    cancellationToken);
            }

            await transaction.CommitAsync(
                cancellationToken);

            return markedCount;
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    public async Task<
        IReadOnlyList<FeeReminderNotification>>
        GetPendingRemindersAsync(
            CancellationToken cancellationToken =
                default)
    {
        const string query = """
            SELECT
                fr.reminder_id,
                fr.invoice_id,
                fr.student_profile_id,
                fr.recipient_user_id,
                fi.invoice_number,
                fi.total_amount,
                fi.paid_amount,
                fi.due_date,
                fr.message,
                fr.notification_status,
                fr.triggered_at
            FROM fee_reminder_notifications AS fr
            INNER JOIN fee_invoices AS fi
                ON fi.invoice_id = fr.invoice_id
            WHERE fr.notification_status = 'PENDING'
            ORDER BY fr.reminder_id;
            """;

        var reminders =
            new List<FeeReminderNotification>();

        await using var connection =
            new MySqlConnection(
                _connectionString);

        await connection.OpenAsync(
            cancellationToken);

        await using var command =
            new MySqlCommand(
                query,
                connection);

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        while (
            await reader.ReadAsync(
                cancellationToken))
        {
            reminders.Add(
                MapReminder(reader));
        }

        return reminders;
    }

    public async Task MarkReminderSentAsync(
        ulong reminderId,
        DateTime sentAt,
        CancellationToken cancellationToken =
            default)
    {
        const string query = """
            UPDATE fee_reminder_notifications
            SET
                notification_status = 'SENT',
                sent_at = @sentAt,
                failure_reason = NULL
            WHERE reminder_id = @reminderId;
            """;

        await ExecuteReminderUpdateAsync(
            query,
            reminderId,
            sentAt,
            null,
            cancellationToken);
    }

    public async Task MarkReminderFailedAsync(
        ulong reminderId,
        string failureReason,
        CancellationToken cancellationToken =
            default)
    {
        const string query = """
            UPDATE fee_reminder_notifications
            SET
                notification_status = 'FAILED',
                failure_reason = @failureReason
            WHERE reminder_id = @reminderId;
            """;

        await ExecuteReminderUpdateAsync(
            query,
            reminderId,
            null,
            failureReason,
            cancellationToken);
    }

    private static async Task<
        IReadOnlyList<OverdueCandidate>>
        GetOverdueCandidatesAsync(
            MySqlConnection connection,
            MySqlTransaction transaction,
            DateOnly processingDate,
            CancellationToken cancellationToken)
    {
        const string query = """
            SELECT
                fi.invoice_id,
                fi.student_profile_id,
                sp.user_id AS recipient_user_id,
                fi.invoice_number,
                fi.total_amount,
                fi.paid_amount,
                fi.due_date
            FROM fee_invoices AS fi
            INNER JOIN student_profiles AS sp
                ON sp.student_profile_id =
                   fi.student_profile_id
            WHERE fi.status = 'UNPAID'
              AND fi.due_date < @processingDate
              AND fi.paid_amount < fi.total_amount
            FOR UPDATE;
            """;

        var candidates =
            new List<OverdueCandidate>();

        await using var command =
            new MySqlCommand(
                query,
                connection,
                transaction);

        command.Parameters.AddWithValue(
            "@processingDate",
            processingDate.ToDateTime(
                TimeOnly.MinValue));

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        while (
            await reader.ReadAsync(
                cancellationToken))
        {
            candidates.Add(
                new OverdueCandidate
                {
                    InvoiceId =
                        reader.GetUInt64(
                            "invoice_id"),

                    StudentProfileId =
                        reader.GetUInt64(
                            "student_profile_id"),

                    RecipientUserId =
                        reader.GetUInt64(
                            "recipient_user_id"),

                    InvoiceNumber =
                        reader.GetString(
                            "invoice_number"),

                    TotalAmount =
                        reader.GetDecimal(
                            "total_amount"),

                    PaidAmount =
                        reader.GetDecimal(
                            "paid_amount"),

                    DueDate =
                        DateOnly.FromDateTime(
                            reader.GetDateTime(
                                "due_date"))
                });
        }

        return candidates;
    }

    private static async Task<int>
        MarkInvoiceOverdueAsync(
            MySqlConnection connection,
            MySqlTransaction transaction,
            ulong invoiceId,
            CancellationToken cancellationToken)
    {
        const string query = """
            UPDATE fee_invoices
            SET status = 'OVERDUE'
            WHERE invoice_id = @invoiceId
              AND status = 'UNPAID'
              AND paid_amount < total_amount;
            """;

        await using var command =
            new MySqlCommand(
                query,
                connection,
                transaction);

        command.Parameters.AddWithValue(
            "@invoiceId",
            invoiceId);

        return await command.ExecuteNonQueryAsync(
            cancellationToken);
    }

    private static async Task
        CreateReminderAsync(
            MySqlConnection connection,
            MySqlTransaction transaction,
            OverdueCandidate candidate,
            CancellationToken cancellationToken)
    {
        const string query = """
            INSERT IGNORE INTO fee_reminder_notifications
            (
                invoice_id,
                student_profile_id,
                recipient_user_id,
                reminder_type,
                message,
                notification_status,
                triggered_at
            )
            VALUES
            (
                @invoiceId,
                @studentProfileId,
                @recipientUserId,
                'OVERDUE_FEE',
                @message,
                'PENDING',
                @triggeredAt
            );
            """;

        decimal outstandingAmount =
            candidate.TotalAmount -
            candidate.PaidAmount;

        string message =
            $"Fee invoice {candidate.InvoiceNumber} is overdue. Outstanding amount: {outstandingAmount:F2}.";

        await using var command =
            new MySqlCommand(
                query,
                connection,
                transaction);

        command.Parameters.AddWithValue(
            "@invoiceId",
            candidate.InvoiceId);

        command.Parameters.AddWithValue(
            "@studentProfileId",
            candidate.StudentProfileId);

        command.Parameters.AddWithValue(
            "@recipientUserId",
            candidate.RecipientUserId);

        command.Parameters.AddWithValue(
            "@message",
            message);

        command.Parameters.AddWithValue(
            "@triggeredAt",
            DateTime.UtcNow);

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }

    private async Task
        ExecuteReminderUpdateAsync(
            string query,
            ulong reminderId,
            DateTime? sentAt,
            string? failureReason,
            CancellationToken cancellationToken)
    {
        await using var connection =
            new MySqlConnection(
                _connectionString);

        await connection.OpenAsync(
            cancellationToken);

        await using var command =
            new MySqlCommand(
                query,
                connection);

        command.Parameters.AddWithValue(
            "@reminderId",
            reminderId);

        command.Parameters.AddWithValue(
            "@sentAt",
            sentAt.HasValue
                ? sentAt.Value
                : DBNull.Value);

        command.Parameters.AddWithValue(
            "@failureReason",
            string.IsNullOrWhiteSpace(
                failureReason)
                ? DBNull.Value
                : failureReason);

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }

    private static FeeReminderNotification
        MapReminder(
            MySqlDataReader reader)
    {
        return new FeeReminderNotification
        {
            ReminderId =
                reader.GetUInt64(
                    "reminder_id"),

            InvoiceId =
                reader.GetUInt64(
                    "invoice_id"),

            StudentProfileId =
                reader.GetUInt64(
                    "student_profile_id"),

            RecipientUserId =
                reader.GetUInt64(
                    "recipient_user_id"),

            InvoiceNumber =
                reader.GetString(
                    "invoice_number"),

            TotalAmount =
                reader.GetDecimal(
                    "total_amount"),

            PaidAmount =
                reader.GetDecimal(
                    "paid_amount"),

            DueDate =
                DateOnly.FromDateTime(
                    reader.GetDateTime(
                        "due_date")),

            Message =
                reader.GetString(
                    "message"),

            NotificationStatus =
                reader.GetString(
                    "notification_status"),

            TriggeredAt =
                reader.GetDateTime(
                    "triggered_at")
        };
    }

    private sealed class OverdueCandidate
    {
        public ulong InvoiceId { get; set; }

        public ulong StudentProfileId { get; set; }

        public ulong RecipientUserId { get; set; }

        public string InvoiceNumber { get; set; }
            = string.Empty;

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public DateOnly DueDate { get; set; }
    }
}