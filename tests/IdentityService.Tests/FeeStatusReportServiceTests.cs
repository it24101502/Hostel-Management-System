using System.Text;
using IdentityService.DTOs;
using IdentityService.Repositories;
using IdentityService.Services;

namespace IdentityService.Tests;

public class FeeStatusReportServiceTests
{
    [Fact]
    public async Task GetReport_WithStudentFilter_PassesStudentIdToRepository()
    {
        var repository =
            new FakeFeeStatusReportRepository();

        var service =
            new FeeStatusReportService(repository);

        IReadOnlyList<FeeStatusReportRow> result =
            await service.GetReportAsync(
                studentProfileId: 10,
                blockId: null);

        Assert.Single(result);

        Assert.Equal(
            (ulong)10,
            repository.LastStudentProfileId);

        Assert.Null(repository.LastBlockId);
    }

    [Fact]
    public async Task GetReport_WithBlockFilter_PassesBlockIdToRepository()
    {
        var repository =
            new FakeFeeStatusReportRepository();

        var service =
            new FeeStatusReportService(repository);

        await service.GetReportAsync(
            studentProfileId: null,
            blockId: 5);

        Assert.Null(
            repository.LastStudentProfileId);

        Assert.Equal(
            (ulong)5,
            repository.LastBlockId);
    }

    [Fact]
    public async Task GenerateCsv_IncludesCurrentFeeStatus()
    {
        var repository =
            new FakeFeeStatusReportRepository
            {
                Rows =
                [
                    CreateSampleRow(
                        invoiceNumber: "INV-001",
                        status: "UNPAID",
                        totalAmount: 25000m,
                        paidAmount: 10000m),

                    CreateSampleRow(
                        invoiceNumber: "INV-002",
                        status: "PAID",
                        totalAmount: 25000m,
                        paidAmount: 25000m),

                    CreateSampleRow(
                        invoiceNumber: "INV-003",
                        status: "OVERDUE",
                        totalAmount: 5000m,
                        paidAmount: 0m)
                ]
            };

        var service =
            new FeeStatusReportService(repository);

        byte[] result =
            await service.GenerateCsvAsync(
                studentProfileId: null,
                blockId: null);

        string csv =
            Encoding.UTF8.GetString(result);

        Assert.Contains("INV-001", csv);
        Assert.Contains("UNPAID", csv);

        Assert.Contains("INV-002", csv);
        Assert.Contains("PAID", csv);

        Assert.Contains("INV-003", csv);
        Assert.Contains("OVERDUE", csv);

        Assert.Contains("15000.00", csv);
    }

    [Fact]
    public async Task GenerateCsv_EscapesValuesContainingComma()
    {
        var repository =
            new FakeFeeStatusReportRepository
            {
                Rows =
                [
                    CreateSampleRow(
                        invoiceNumber: "INV-004",
                        status: "UNPAID",
                        totalAmount: 10000m,
                        paidAmount: 0m,
                        studentName: "Perera, Kamal")
                ]
            };

        var service =
            new FeeStatusReportService(repository);

        byte[] result =
            await service.GenerateCsvAsync(
                studentProfileId: null,
                blockId: null);

        string csv =
            Encoding.UTF8.GetString(result);

        Assert.Contains(
            "\"Perera, Kamal\"",
            csv);
    }

    [Fact]
    public async Task GenerateCsv_WithNoRecords_StillContainsHeader()
    {
        var repository =
            new FakeFeeStatusReportRepository
            {
                Rows = []
            };

        var service =
            new FeeStatusReportService(repository);

        byte[] result =
            await service.GenerateCsvAsync(
                studentProfileId: null,
                blockId: null);

        string csv =
            Encoding.UTF8.GetString(result);

        Assert.Contains(
            "Invoice Number",
            csv);

        Assert.Contains(
            "Registration Number",
            csv);

        Assert.Contains(
            "Status",
            csv);
    }

    private static FeeStatusReportRow CreateSampleRow(
        string invoiceNumber,
        string status,
        decimal totalAmount,
        decimal paidAmount,
        string studentName = "Test Student")
    {
        return new FeeStatusReportRow
        {
            InvoiceId = 1,
            InvoiceNumber = invoiceNumber,
            StudentProfileId = 10,
            RegistrationNumber = "IT26000001",
            StudentName = studentName,
            Email = "student@example.com",
            BlockId = 5,
            BlockCode = "A",
            BlockName = "Hostel Block A",
            FeeType = "HOSTEL_FEE",
            TotalAmount = totalAmount,
            PaidAmount = paidAmount,

            OutstandingAmount =
                Math.Max(
                    totalAmount - paidAmount,
                    0m),

            IssuedAt =
                new DateTime(
                    2026,
                    8,
                    20,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc),

            DueDate =
                new DateOnly(
                    2026,
                    8,
                    27),

            Status = status
        };
    }

    private sealed class FakeFeeStatusReportRepository
        : IFeeStatusReportRepository
    {
        public ulong? LastStudentProfileId
        {
            get;
            private set;
        }

        public ulong? LastBlockId
        {
            get;
            private set;
        }

        public IReadOnlyList<FeeStatusReportRow> Rows
        {
            get;
            set;
        } =
        [
            CreateSampleRow(
                invoiceNumber: "INV-TEST",
                status: "UNPAID",
                totalAmount: 10000m,
                paidAmount: 0m)
        ];

        public Task<IReadOnlyList<FeeStatusReportRow>>
            GetReportAsync(
                ulong? studentProfileId,
                ulong? blockId)
        {
            LastStudentProfileId =
                studentProfileId;

            LastBlockId =
                blockId;

            return Task.FromResult(Rows);
        }
    }
}