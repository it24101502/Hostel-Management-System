using IdentityService.Authorization;
using IdentityService.DTOs;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/admin/fee-reports")]
[RequireRole("ADMIN")]
public class FeeStatusReportsController
    : ControllerBase
{
    private readonly IFeeStatusReportService
        _reportService;

    public FeeStatusReportsController(
        IFeeStatusReportService reportService)
    {
        _reportService = reportService;
    }

    // View all records or filter by student/block.
    //
    // GET /api/admin/fee-reports
    // GET /api/admin/fee-reports?studentProfileId=1
    // GET /api/admin/fee-reports?blockId=1
    [HttpGet]
    public async Task<
        ActionResult<IReadOnlyList<FeeStatusReportRow>>>
        GetReport(
            [FromQuery] ulong? studentProfileId,
            [FromQuery] ulong? blockId)
    {
        ActionResult? validationResult =
            ValidateFilters(
                studentProfileId,
                blockId);

        if (validationResult is not null)
        {
            return validationResult;
        }

        IReadOnlyList<FeeStatusReportRow> report =
            await _reportService.GetReportAsync(
                studentProfileId,
                blockId);

        return Ok(report);
    }

    // Download the same filtered report as CSV.
    //
    // GET /api/admin/fee-reports/csv
    // GET /api/admin/fee-reports/csv?studentProfileId=1
    // GET /api/admin/fee-reports/csv?blockId=1
    [HttpGet("csv")]
    public async Task<IActionResult>
        DownloadCsv(
            [FromQuery] ulong? studentProfileId,
            [FromQuery] ulong? blockId)
    {
        ActionResult? validationResult =
            ValidateFilters(
                studentProfileId,
                blockId);

        if (validationResult is not null)
        {
            return validationResult;
        }

        byte[] csv =
            await _reportService.GenerateCsvAsync(
                studentProfileId,
                blockId);

        string fileName =
            $"fee-status-report-" +
            $"{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

        return File(
            csv,
            "text/csv; charset=utf-8",
            fileName);
    }

    private ActionResult? ValidateFilters(
        ulong? studentProfileId,
        ulong? blockId)
    {
        if (studentProfileId == 0)
        {
            return BadRequest(new
            {
                field = "studentProfileId",
                message =
                    "Student profile ID must be greater than zero."
            });
        }

        if (blockId == 0)
        {
            return BadRequest(new
            {
                field = "blockId",
                message =
                    "Hostel block ID must be greater than zero."
            });
        }

        return null;
    }
}