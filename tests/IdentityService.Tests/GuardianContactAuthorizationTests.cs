using System.Security.Claims;
using IdentityService.Controllers;
using IdentityService.DTOs;
using IdentityService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace IdentityService.Tests;

public class GuardianContactAuthorizationTests
{
    [Fact]
    public async Task Student_AccessingOwnContacts_IsAllowed()
    {
        var contactService = new FakeContactService();

        var profileService = new FakeProfileService
        {
            OwnProfile = new StudentProfileResponse
            {
                StudentProfileId = 10,
                UserId = 100
            }
        };

        GuardianContactsController controller =
            CreateController(
                contactService,
                profileService,
                "STUDENT",
                100);

        ActionResult<
            IReadOnlyList<GuardianContactResponse>> result =
                await controller.GetContacts(10);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(contactService.WasCalled);
    }

    [Fact]
    public async Task Student_AccessingAnotherStudentsContacts_Returns403()
    {
        var contactService = new FakeContactService();

        var profileService = new FakeProfileService
        {
            OwnProfile = new StudentProfileResponse
            {
                StudentProfileId = 10,
                UserId = 100
            }
        };

        GuardianContactsController controller =
            CreateController(
                contactService,
                profileService,
                "STUDENT",
                100);

        // Profile 99 does not belong to this student.
        ActionResult<
            IReadOnlyList<GuardianContactResponse>> result =
                await controller.GetContacts(99);

        ObjectResult forbiddenResult =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            forbiddenResult.StatusCode);

        // The service must not retrieve another student's data.
        Assert.False(contactService.WasCalled);
    }

    [Fact]
    public async Task Administrator_AccessingAnyStudentsContacts_IsAllowed()
    {
        var contactService = new FakeContactService();
        var profileService = new FakeProfileService();

        GuardianContactsController controller =
            CreateController(
                contactService,
                profileService,
                "ADMIN",
                1);

        ActionResult<
            IReadOnlyList<GuardianContactResponse>> result =
                await controller.GetContacts(99);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(contactService.WasCalled);
    }

    private static GuardianContactsController CreateController(
        IGuardianContactService contactService,
        IStudentProfileService profileService,
        string role,
        ulong userId)
    {
        Claim[] claims =
        [
            new Claim(
                JwtRegisteredClaimNames.Sub,
                userId.ToString()),

            new Claim(
                ClaimTypes.Role,
                role)
        ];

        var identity = new ClaimsIdentity(
            claims,
            "TestAuthentication",
            ClaimTypes.Name,
            ClaimTypes.Role);

        var controller =
            new GuardianContactsController(
                contactService,
                profileService);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext
                    {
                        User =
                            new ClaimsPrincipal(identity)
                    }
            };

        return controller;
    }

    private sealed class FakeContactService
        : IGuardianContactService
    {
        public bool WasCalled { get; private set; }

        public Task<
            IReadOnlyList<GuardianContactResponse>>
            GetByStudentProfileIdAsync(
                ulong studentProfileId)
        {
            WasCalled = true;

            IReadOnlyList<GuardianContactResponse> contacts =
                Array.Empty<GuardianContactResponse>();

            return Task.FromResult(contacts);
        }

        public Task<GuardianContactResponse?>
            GetByIdAsync(
                ulong studentProfileId,
                ulong contactId)
        {
            throw new NotSupportedException();
        }

        public Task<GuardianContactResponse>
            CreateAsync(
                ulong studentProfileId,
                CreateGuardianContactRequest request)
        {
            throw new NotSupportedException();
        }

        public Task<GuardianContactResponse?>
            UpdateAsync(
                ulong studentProfileId,
                ulong contactId,
                UpdateGuardianContactRequest request)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeProfileService
        : IStudentProfileService
    {
        public StudentProfileResponse? OwnProfile { get; init; }

        public Task<StudentProfileResponse?> GetOwnAsync(
            ulong userId)
        {
            return Task.FromResult(OwnProfile);
        }

        public Task<StudentProfileResponse?> GetByIdAsync(
            ulong studentProfileId)
        {
            throw new NotSupportedException();
        }

        public Task<StudentProfileResponse> CreateAsync(
            CreateStudentProfileRequest request)
        {
            throw new NotSupportedException();
        }

        public Task<StudentProfileResponse?> UpdateAsync(
            ulong studentProfileId,
            UpdateStudentProfileRequest request)
        {
            throw new NotSupportedException();
        }

        public Task<StudentProfileResponse?> UpdateOwnAsync(
            ulong userId,
            UpdateOwnStudentProfileRequest request)
        {
            throw new NotSupportedException();
        }

        public Task<StudentProfileResponse?> UpdateOwnPhotoAsync(
            ulong userId,
            string profilePhotoUrl)
        {
            throw new NotSupportedException();
        }
    }
}