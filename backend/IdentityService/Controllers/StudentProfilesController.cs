using IdentityService.Authorization;
using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/student-profiles")]
public class StudentProfilesController : ControllerBase
{
    private readonly IStudentProfileService _profileService;

    public StudentProfilesController(
        IStudentProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("{studentProfileId:long}")]
    [RequireRole(
        "STUDENT",
        "ADMIN",
        "WARDEN",
        "HOSTEL_MASTER")]
    public async Task<ActionResult<StudentProfileResponse>>
        GetProfile(ulong studentProfileId)
    {
        StudentProfileResponse? profile =
            await _profileService.GetByIdAsync(
                studentProfileId);

        if (profile is null)
        {
            return NotFound(new
            {
                message = "Student profile was not found."
            });
        }

        return Ok(profile);
    }

    [HttpPost]
    [RequireRole("ADMIN")]
    public async Task<ActionResult<StudentProfileResponse>>
        CreateProfile(
            CreateStudentProfileRequest request)
    {
        try
        {
            StudentProfileResponse createdProfile =
                await _profileService.CreateAsync(
                    request);

            return CreatedAtAction(
                nameof(GetProfile),
                new
                {
                    studentProfileId =
                        createdProfile.StudentProfileId
                },
                createdProfile);
        }
        catch (DuplicateEmailException exception)
        {
            return Conflict(new
            {
                field = "email",
                message = exception.Message
            });
        }
        catch (
            DuplicateRegistrationNumberException exception)
        {
            return Conflict(new
            {
                field = "registrationNumber",
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
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
    }

    [HttpPut("{studentProfileId:long}")]
    [RequireRole(
        "STUDENT",
        "ADMIN",
        "WARDEN",
        "HOSTEL_MASTER")]
    public async Task<ActionResult<StudentProfileResponse>>
        UpdateProfile(
            ulong studentProfileId,
            UpdateStudentProfileRequest request)
    {
        try
        {
            StudentProfileResponse? updatedProfile =
                await _profileService.UpdateAsync(
                    studentProfileId,
                    request);

            if (updatedProfile is null)
            {
                return NotFound(new
                {
                    message =
                        "Student profile was not found."
                });
            }

            return Ok(updatedProfile);
        }
        catch (DuplicateEmailException exception)
        {
            return Conflict(new
            {
                field = "email",
                message = exception.Message
            });
        }
        catch (
            DuplicateRegistrationNumberException exception)
        {
            return Conflict(new
            {
                field = "registrationNumber",
                message = exception.Message
            });
        }
    }
}