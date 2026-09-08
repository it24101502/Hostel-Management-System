using IdentityService.Authorization;
using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/fee-invoices")]
[RequireRole("ADMIN")]
public class FeeInvoicesController
    : ControllerBase
{
    private readonly IFeeInvoiceService
        _feeInvoiceService;

    public FeeInvoicesController(
        IFeeInvoiceService feeInvoiceService)
    {
        _feeInvoiceService =
            feeInvoiceService;
    }

    [HttpPost]
    public async Task<
        ActionResult<FeeInvoiceResponse>>
        CreateInvoice(
            [FromBody]
            CreateFeeInvoiceRequest request)
    {
        try
        {
            FeeInvoiceResponse createdInvoice =
                await _feeInvoiceService
                    .CreateAsync(request);

            return StatusCode(
                StatusCodes.Status201Created,
                createdInvoice);
        }
        catch (
            FeeInvoiceValidationException
            exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
    }
}