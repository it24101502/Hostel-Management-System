using IdentityService.DTOs;
using IdentityService.Authorization;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/student-profiles/{studentProfileId:long}/contacts")]
[RequireRole("STUDENT", "ADMIN", "WARDEN", "HOSTEL_MASTER")]
public class GuardianContactsController : ControllerBase
{
    private readonly IGuardianContactService _contactService;
    private readonly IStudentProfileService _profileService;

    public GuardianContactsController(
        IGuardianContactService contactService,
        IStudentProfileService profileService)
    {
        _contactService = contactService;
        _profileService = profileService;
    }

    // Retrieve all guardian and emergency contacts
    [HttpGet]
    public async Task<ActionResult<
        IReadOnlyList<GuardianContactResponse>>> GetContacts(
        ulong studentProfileId)
    {
        ActionResult? authorizationResult =
            await AuthorizeProfileAccessAsync(studentProfileId);

        if (authorizationResult is not null)
        {
            return authorizationResult;
        }
        try
        {
            IReadOnlyList<GuardianContactResponse> contacts =
                await _contactService
                    .GetByStudentProfileIdAsync(studentProfileId);

            return Ok(contacts);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
    }

    // Retrieve one contact
    [HttpGet("{contactId:long}")]
    public async Task<ActionResult<GuardianContactResponse>>
        GetContact(
            ulong studentProfileId,
            ulong contactId)
    {
        ActionResult? authorizationResult =
            await AuthorizeProfileAccessAsync(studentProfileId);

        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        GuardianContactResponse? contact =
            await _contactService.GetByIdAsync(
                studentProfileId,
                contactId);

        if (contact is null)
        {
            return NotFound(new
            {
                message = "Guardian or emergency contact was not found."
            });
        }

        return Ok(contact);
    }

    // Add a guardian or emergency contact
    [HttpPost]
    public async Task<ActionResult<GuardianContactResponse>>
        CreateContact(
            ulong studentProfileId,
            CreateGuardianContactRequest request)
    {
        ActionResult? authorizationResult =
            await AuthorizeProfileAccessAsync(studentProfileId);

        if (authorizationResult is not null)
        {
            return authorizationResult;
        }
        try
        {
            GuardianContactResponse createdContact =
                await _contactService.CreateAsync(
                    studentProfileId,
                    request);

            return CreatedAtAction(
                nameof(GetContact),
                new
                {
                    studentProfileId,
                    contactId =
                        createdContact.GuardianContactId
                },
                createdContact);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
    }

    // Edit an existing contact
    [HttpPut("{contactId:long}")]
    public async Task<ActionResult<GuardianContactResponse>>
        UpdateContact(
            ulong studentProfileId,
            ulong contactId,
            UpdateGuardianContactRequest request)
    {
        ActionResult? authorizationResult =
            await AuthorizeProfileAccessAsync(studentProfileId);

        if (authorizationResult is not null)
        {
            return authorizationResult;
        }
        GuardianContactResponse? updatedContact =
            await _contactService.UpdateAsync(
                studentProfileId,
                contactId,
                request);

        if (updatedContact is null)
        {
            return NotFound(new
            {
                message = "Guardian or emergency contact was not found."
            });
        }

        return Ok(updatedContact);
    }
    private async Task<ActionResult?>
        AuthorizeProfileAccessAsync(
            ulong studentProfileId)
    {
        // Staff can access contacts belonging to any student.
        if (User.IsInRole("ADMIN") ||
            User.IsInRole("WARDEN") ||
            User.IsInRole("HOSTEL_MASTER"))
        {
            return null;
        }

        string? userIdValue =
            User.FindFirst(
                JwtRegisteredClaimNames.Sub)?.Value;

        if (!ulong.TryParse(userIdValue, out ulong userId))
        {
            return Unauthorized(new
            {
                message =
                    "The authenticated user ID is missing or invalid."
            });
        }

        StudentProfileResponse? ownProfile =
            await _profileService.GetOwnAsync(userId);

        if (ownProfile?.StudentProfileId != studentProfileId)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message =
                        "Students may access only their own guardian and emergency contacts."
                });
        }

        return null;
    }
}