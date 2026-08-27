using IdentityService.Authorization;
using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/student-profiles")]
public class StudentProfilesController : ControllerBase
{
    private readonly IStudentProfileService _profileService;

    private readonly IWebHostEnvironment _environment;

    private const long MaximumPhotoSize =
        2 * 1024 * 1024;

    public StudentProfilesController(
        IStudentProfileService profileService,
        IWebHostEnvironment environment)
    {
        _profileService = profileService;
        _environment = environment;
    }

    // Student retrieves their own profile.
    [HttpGet("me")]
    [RequireRole("STUDENT")]
    public async Task<ActionResult<StudentProfileResponse>>
        GetOwnProfile()
    {
        if (!TryGetAuthenticatedUserId(out ulong userId))
        {
            return Unauthorized(new
            {
                message =
                    "The authenticated user ID is missing or invalid."
            });
        }

        StudentProfileResponse? profile =
            await _profileService.GetOwnAsync(userId);

        if (profile is null)
        {
            return NotFound(new
            {
                message = "Student profile was not found."
            });
        }

        return Ok(profile);
    }

    // Student updates only their permitted fields.
    [HttpPut("me")]
    [RequireRole("STUDENT")]
    public async Task<ActionResult<StudentProfileResponse>>
        UpdateOwnProfile(
            UpdateOwnStudentProfileRequest request)
    {
        if (!TryGetAuthenticatedUserId(out ulong userId))
        {
            return Unauthorized(new
            {
                message =
                    "The authenticated user ID is missing or invalid."
            });
        }

        try
        {
            StudentProfileResponse? updatedProfile =
                await _profileService.UpdateOwnAsync(
                    userId,
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
        catch (RestrictedProfileFieldException exception)
        {
            return BadRequest(new
            {
                message = exception.Message,
                restrictedFields =
                    exception.RestrictedFields
            });
        }
    }

    [HttpPost("me/photo")]
    [RequireRole("STUDENT")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(2_200_000)]
    public async Task<ActionResult<StudentProfileResponse>>
        UploadOwnProfilePhoto(
            [FromForm] IFormFile? photo)
    {
        if (!TryGetAuthenticatedUserId(out ulong userId))
        {
            return Unauthorized(new
            {
                message =
                    "The authenticated user ID is missing or invalid."
            });
        }

        if (photo is null || photo.Length == 0)
        {
            return BadRequest(new
            {
                message = "Please select a photo."
            });
        }

        if (photo.Length > MaximumPhotoSize)
        {
            return BadRequest(new
            {
                message =
                    "The profile photo must not exceed 2 MB."
            });
        }

        string? extension =
            photo.ContentType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => null
            };

        if (extension is null)
        {
            return BadRequest(new
            {
                message =
                    "Only JPG, PNG and WebP images are permitted."
            });
        }

        StudentProfileResponse? existingProfile =
            await _profileService.GetOwnAsync(userId);

        if (existingProfile is null)
        {
            return NotFound(new
            {
                message = "Student profile was not found."
            });
        }

        string webRootPath =
            _environment.WebRootPath ??
            Path.Combine(
                _environment.ContentRootPath,
                "wwwroot");

        string uploadDirectory =
            Path.Combine(
                webRootPath,
                "uploads",
                "profile-photos");

        Directory.CreateDirectory(uploadDirectory);

        string fileName =
            $"{Guid.NewGuid():N}{extension}";

        string filePath =
            Path.Combine(
                uploadDirectory,
                fileName);

        try
        {
            await using (
                var fileStream =
                    new FileStream(
                        filePath,
                        FileMode.CreateNew))
            {
                await photo.CopyToAsync(fileStream);
            }

            string photoUrl =
                $"{Request.Scheme}://" +
                $"{Request.Host}" +
                $"/uploads/profile-photos/{fileName}";

            StudentProfileResponse? updatedProfile =
                await _profileService
                    .UpdateOwnPhotoAsync(
                        userId,
                        photoUrl);

            if (updatedProfile is null)
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                return NotFound(new
                {
                    message = "Student profile was not found."
                });
            }

            return Ok(updatedProfile);
        }
        catch
        {
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            throw;
        }
    }

    // Staff retrieves a profile using its ID.
    [HttpGet("{studentProfileId:long}")]
    [RequireRole(
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

    // Administrator creates a student profile.
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

    // Staff updates a profile using its ID.
    [HttpPut("{studentProfileId:long}")]
    [RequireRole(
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

    private bool TryGetAuthenticatedUserId(
        out ulong userId)
    {
        string? userIdValue =
            User.FindFirst(
                JwtRegisteredClaimNames.Sub)?.Value;

        return ulong.TryParse(
            userIdValue,
            out userId);
    }
}