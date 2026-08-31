using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Models;
using IdentityService.Services;
using MySqlConnector;

namespace IdentityService.Repositories;

public class FeePaymentRepository
    : IFeePaymentRepository
{
    private readonly string _connectionString;

    public FeePaymentRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString(
                "DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task<FeeInvoicePaymentState?>
        GetInvoiceStateAsync(
            ulong invoiceId)
    {
        const string query = """
            SELECT
                invoice_id,
                total_amount,
                paid_amount,
                due_date,
                status
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

        return MapInvoiceState(reader);
    }

    public async Task<RecordFeePaymentResponse>
        RecordAsync(
            ulong invoiceId,
            ulong recordedByUserId,
            string paymentReference,
            RecordFeePaymentRequest request)
    {
        await using var connection =
            new MySqlConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var transaction =
            await connection
                .BeginTransactionAsync();

        try
        {
            FeeInvoicePaymentState invoice =
                await GetLockedInvoiceAsync(
                    connection,
                    transaction,
                    invoiceId);

            ValidatePaymentAgainstInvoice(
                invoice,
                request.Amount!.Value);

            decimal newPaidAmount =
                invoice.PaidAmount +
                request.Amount.Value;

            DateOnly today =
                DateOnly.FromDateTime(
                    DateTime.UtcNow);

            string newInvoiceStatus =
                FeeInvoiceStatusCalculator.Determine(
                    invoice.TotalAmount,
                    newPaidAmount,
                    invoice.DueDate,
                    today);

            DateTime paidAt =
                DateTime.UtcNow;

            ulong paymentId =
                await InsertPaymentAsync(
                    connection,
                    transaction,
                    invoiceId,
                    recordedByUserId,
                    paymentReference,
                    request,
                    paidAt);

            await UpdateInvoiceAsync(
                connection,
                transaction,
                invoiceId,
                newPaidAmount,
                newInvoiceStatus);

            await transaction.CommitAsync();

            return new RecordFeePaymentResponse
            {
                PaymentId = paymentId,

                InvoiceId = invoiceId,

                PaymentReference =
                    paymentReference,

                PaymentAmount =
                    request.Amount.Value,

                PaymentMethod =
                    request.PaymentMethod,

                PaymentStatus =
                    "COMPLETED",

                PaidAt = paidAt,

                TotalAmount =
                    invoice.TotalAmount,

                PaidAmount =
                    newPaidAmount,

                OutstandingAmount =
                    invoice.TotalAmount -
                    newPaidAmount,

                InvoiceStatus =
                    newInvoiceStatus
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task<
        FeeInvoicePaymentState>
        GetLockedInvoiceAsync(
            MySqlConnection connection,
            MySqlTransaction transaction,
            ulong invoiceId)
    {
        const string query = """
            SELECT
                invoice_id,
                total_amount,
                paid_amount,
                due_date,
                status
            FROM fee_invoices
            WHERE invoice_id = @invoiceId
            FOR UPDATE;
            """;

        await using var command =
            new MySqlCommand(
                query,
                connection,
                transaction);

        command.Parameters.AddWithValue(
            "@invoiceId",
            invoiceId);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new KeyNotFoundException(
                "Fee invoice was not found.");
        }

        return MapInvoiceState(reader);
    }

    private static async Task<ulong>
        InsertPaymentAsync(
            MySqlConnection connection,
            MySqlTransaction transaction,
            ulong invoiceId,
            ulong recordedByUserId,
            string paymentReference,
            RecordFeePaymentRequest request,
            DateTime paidAt)
    {
        const string query = """
            INSERT INTO fee_payments
            (
                invoice_id,
                payment_reference,
                amount,
                payment_method,
                payment_status,
                paid_at,
                recorded_by_user_id,
                notes
            )
            VALUES
            (
                @invoiceId,
                @paymentReference,
                @amount,
                @paymentMethod,
                'COMPLETED',
                @paidAt,
                @recordedByUserId,
                @notes
            );
            """;

        await using var command =
            new MySqlCommand(
                query,
                connection,
                transaction);

        command.Parameters.AddWithValue(
            "@invoiceId",
            invoiceId);

        command.Parameters.AddWithValue(
            "@paymentReference",
            paymentReference);

        command.Parameters.AddWithValue(
            "@amount",
            request.Amount!.Value);

        command.Parameters.AddWithValue(
            "@paymentMethod",
            request.PaymentMethod);

        command.Parameters.AddWithValue(
            "@paidAt",
            paidAt);

        command.Parameters.AddWithValue(
            "@recordedByUserId",
            recordedByUserId);

        command.Parameters.AddWithValue(
            "@notes",
            string.IsNullOrWhiteSpace(
                request.Notes)
                ? DBNull.Value
                : request.Notes.Trim());

        await command.ExecuteNonQueryAsync();

        return (ulong)command.LastInsertedId;
    }

    private static async Task
        UpdateInvoiceAsync(
            MySqlConnection connection,
            MySqlTransaction transaction,
            ulong invoiceId,
            decimal newPaidAmount,
            string newStatus)
    {
        const string query = """
            UPDATE fee_invoices
            SET
                paid_amount = @paidAmount,
                status = @status
            WHERE invoice_id = @invoiceId;
            """;

        await using var command =
            new MySqlCommand(
                query,
                connection,
                transaction);

        command.Parameters.AddWithValue(
            "@paidAmount",
            newPaidAmount);

        command.Parameters.AddWithValue(
            "@status",
            newStatus);

        command.Parameters.AddWithValue(
            "@invoiceId",
            invoiceId);

        await command.ExecuteNonQueryAsync();
    }

    private static void
        ValidatePaymentAgainstInvoice(
            FeeInvoicePaymentState invoice,
            decimal paymentAmount)
    {
        if (
            invoice.Status.Equals(
                "CANCELLED",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new FeePaymentValidationException(
                "Payments cannot be recorded for a cancelled invoice.");
        }

        if (
            invoice.Status.Equals(
                "PAID",
                StringComparison.OrdinalIgnoreCase) ||
            invoice.PaidAmount >=
                invoice.TotalAmount)
        {
            throw new FeePaymentValidationException(
                "The invoice is already fully paid.");
        }

        decimal outstandingAmount =
            invoice.TotalAmount -
            invoice.PaidAmount;

        if (paymentAmount > outstandingAmount)
        {
            throw new FeePaymentValidationException(
                $"Payment amount cannot exceed the outstanding amount of {outstandingAmount:F2}.");
        }
    }

    private static FeeInvoicePaymentState
        MapInvoiceState(
            MySqlDataReader reader)
    {
        return new FeeInvoicePaymentState
        {
            InvoiceId =
                reader.GetUInt64(
                    "invoice_id"),

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

            Status =
                reader.GetString(
                    "status")
        };
    }
}