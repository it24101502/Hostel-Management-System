using IdentityService.DTOs;
using IdentityService.Authorization;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/student-profiles/{studentProfileId:long}/contacts")]
[RequireRole("STUDENT", "ADMIN", "WARDEN", "HOSTEL_MASTER")]
public class GuardianContactsController : ControllerBase
{
    private readonly IGuardianContactService _contactService;

    public GuardianContactsController(
        IGuardianContactService contactService)
    {
        _contactService = contactService;
    }

    // Retrieve all guardian and emergency contacts
    [HttpGet]
    public async Task<ActionResult<
        IReadOnlyList<GuardianContactResponse>>> GetContacts(
        ulong studentProfileId)
    {
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
}