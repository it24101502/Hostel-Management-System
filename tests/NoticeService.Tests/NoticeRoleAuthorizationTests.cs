using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using NoticeService.Controllers;
using Xunit;

namespace NoticeService.Tests;

/// <summary>
/// NoticesController is protected with [Authorize(Roles = ...)], so these
/// tests build the real ASP.NET Core policy for each action from its
/// attributes and evaluate it for each role. A student must never reach a
/// staff action, and an anonymous caller must never reach any action.
/// </summary>
public class NoticeRoleAuthorizationTests
{
    private static readonly string[] StaffRoles =
        { "WARDEN", "HOSTEL_MASTER", "ADMIN" };

    private static readonly string[] StaffActions =
    {
        nameof(NoticesController.CreateNotice),
        nameof(NoticesController.GetAllNotices),
        nameof(NoticesController.GetNoticeById),
        nameof(NoticesController.GetStudentNotices),
        nameof(NoticesController.UpdateNotice),
        nameof(NoticesController.DeleteNotice)
    };

    public static IEnumerable<object?[]> AccessMatrix()
    {
        foreach (string action in StaffActions)
        {
            foreach (string role in StaffRoles)
            {
                yield return new object?[] { action, role, true };
            }

            yield return new object?[] { action, "STUDENT", false };
            yield return new object?[] { action, "UNKNOWN_ROLE", false };
            yield return new object?[] { action, null, false };
        }

        yield return new object?[]
            { nameof(NoticesController.GetMyNotices), "STUDENT", true };

        foreach (string role in StaffRoles)
        {
            yield return new object?[]
                { nameof(NoticesController.GetMyNotices), role, false };
        }

        yield return new object?[]
            { nameof(NoticesController.GetMyNotices), null, false };
    }

    [Theory]
    [MemberData(nameof(AccessMatrix))]
    public async Task Action_IsAllowedOnlyForTheIntendedRoles(
        string action,
        string? role,
        bool expectedAllowed)
    {
        bool allowed = await IsAllowedAsync(action, role);

        Assert.Equal(expectedAllowed, allowed);
    }

    [Fact]
    public void Controller_RequiresAnAuthenticatedUser()
    {
        var attribute =
            typeof(NoticesController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
    }

    [Fact]
    public void EveryAction_DeclaresItsOwnRoleRestriction()
    {
        var actions = typeof(NoticesController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes()
                .Any(attribute => attribute is HttpMethodAttribute))
            .ToList();

        Assert.NotEmpty(actions);

        foreach (MethodInfo action in actions)
        {
            var authorize = action.GetCustomAttribute<AuthorizeAttribute>();

            Assert.True(
                authorize is not null &&
                !string.IsNullOrWhiteSpace(authorize.Roles),
                $"{action.Name} must declare [Authorize(Roles = ...)].");
        }
    }

    [Fact]
    public void StudentEndpoint_IsNotOpenToStaff_AndStaffEndpointsAreNotOpenToStudents()
    {
        string studentRoles = RolesOf(nameof(NoticesController.GetMyNotices));

        Assert.Equal("STUDENT", studentRoles);

        foreach (string action in StaffActions)
        {
            Assert.DoesNotContain("STUDENT", RolesOf(action).Split(','));
        }
    }

    private static string RolesOf(string action) =>
        typeof(NoticesController)
            .GetMethod(action)!
            .GetCustomAttribute<AuthorizeAttribute>()!
            .Roles!;

    private static async Task<bool> IsAllowedAsync(string action, string? role)
    {
        MethodInfo method = typeof(NoticesController).GetMethod(action)!;

        List<IAuthorizeData> authorizeData =
            typeof(NoticesController)
                .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
                .Cast<IAuthorizeData>()
                .Concat(
                    method
                        .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
                        .Cast<IAuthorizeData>())
                .ToList();

        using ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddAuthorization()
            .BuildServiceProvider();

        var policyProvider =
            services.GetRequiredService<IAuthorizationPolicyProvider>();

        AuthorizationPolicy policy =
            (await AuthorizationPolicy.CombineAsync(
                policyProvider,
                authorizeData))!;

        var authorization =
            services.GetRequiredService<IAuthorizationService>();

        ClaimsPrincipal user = role is null
            ? new ClaimsPrincipal(new ClaimsIdentity())
            : new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Role, role) },
                    authenticationType: "Test"));

        AuthorizationResult result =
            await authorization.AuthorizeAsync(user, policy);

        return result.Succeeded;
    }
}