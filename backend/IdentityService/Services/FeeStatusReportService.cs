using System.Globalization;
using System.Text;
using IdentityService.DTOs;
using IdentityService.Repositories;

namespace IdentityService.Services;

public class FeeStatusReportService
    : IFeeStatusReportService
{
    private readonly IFeeStatusReportRepository
        _reportRepository;

    public FeeStatusReportService(
        IFeeStatusReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public Task<IReadOnlyList<FeeStatusReportRow>>
        GetReportAsync(
            ulong? studentProfileId,
            ulong? blockId)
    {
        return _reportRepository.GetReportAsync(
            studentProfileId,
            blockId);
    }

    public async Task<byte[]> GenerateCsvAsync(
        ulong? studentProfileId,
        ulong? blockId)
    {
        IReadOnlyList<FeeStatusReportRow> rows =
            await GetReportAsync(
                studentProfileId,
                blockId);

        var csv = new StringBuilder();

        csv.AppendLine(
            "Invoice Number," +
            "Registration Number," +
            "Student Name," +
            "Email," +
            "Block Code," +
            "Block Name," +
            "Fee Type," +
            "Total Amount," +
            "Paid Amount," +
            "Outstanding Amount," +
            "Issued At," +
            "Due Date," +
            "Status");

        foreach (FeeStatusReportRow row in rows)
        {
            string[] values =
            [
                row.InvoiceNumber,
                row.RegistrationNumber,
                row.StudentName,
                row.Email,
                row.BlockCode ?? "Not assigned",
                row.BlockName ?? "Not assigned",
                row.FeeType,

                row.TotalAmount.ToString(
                    "0.00",
                    CultureInfo.InvariantCulture),

                row.PaidAmount.ToString(
                    "0.00",
                    CultureInfo.InvariantCulture),

                row.OutstandingAmount.ToString(
                    "0.00",
                    CultureInfo.InvariantCulture),

                row.IssuedAt.ToString(
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture),

                row.DueDate.ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture),

                row.Status
            ];

            csv.AppendLine(
                string.Join(
                    ",",
                    values.Select(EscapeCsvValue)));
        }

        // Add the UTF-8 BOM so Microsoft Excel
        // displays text correctly.
        byte[] preamble =
            Encoding.UTF8.GetPreamble();

        byte[] content =
            Encoding.UTF8.GetBytes(
                csv.ToString());

        byte[] result =
            new byte[
                preamble.Length + content.Length];

        Buffer.BlockCopy(
            preamble,
            0,
            result,
            0,
            preamble.Length);

        Buffer.BlockCopy(
            content,
            0,
            result,
            preamble.Length,
            content.Length);

        return result;
    }

    private static string EscapeCsvValue(
        string? value)
    {
        string safeValue = value ?? string.Empty;

        bool requiresQuotes =
            safeValue.Contains(',') ||
            safeValue.Contains('"') ||
            safeValue.Contains('\r') ||
            safeValue.Contains('\n');

        if (!requiresQuotes)
        {
            return safeValue;
        }

        return
            $"\"{safeValue.Replace("\"", "\"\"")}\"";
    }
}