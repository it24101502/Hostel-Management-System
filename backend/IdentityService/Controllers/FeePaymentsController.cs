using IdentityService.Authorization;
using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace IdentityService.Controllers;

[ApiController]
[Route(
    "api/fee-invoices/{invoiceId:long}/payments")]
[RequireRole("ADMIN")]
public class FeePaymentsController
    : ControllerBase
{
    private readonly IFeePaymentService
        _paymentService;

    public FeePaymentsController(
        IFeePaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<
        ActionResult<RecordFeePaymentResponse>>
        RecordPayment(
            ulong invoiceId,
            [FromBody]
            RecordFeePaymentRequest request)
    {
        if (!TryGetAuthenticatedUserId(
                out ulong administratorId))
        {
            return Unauthorized(new
            {
                message =
                    "The authenticated Administrator ID is missing or invalid."
            });
        }

        try
        {
            RecordFeePaymentResponse result =
                await _paymentService.RecordAsync(
                    invoiceId,
                    administratorId,
                    request);

            return StatusCode(
                StatusCodes.Status201Created,
                result);
        }
        catch (
            FeePaymentValidationException
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

    private bool TryGetAuthenticatedUserId(
        out ulong userId)
    {
        string? userIdValue =
            User.FindFirst(
                JwtRegisteredClaimNames.Sub)
                ?.Value;

        return ulong.TryParse(
            userIdValue,
            out userId);
    }
}