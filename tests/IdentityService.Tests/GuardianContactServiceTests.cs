using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Repositories;
using IdentityService.Services;

namespace IdentityService.Tests;

public class GuardianContactServiceTests
{
    [Fact]
    public async Task CreateContact_WithValidDetails_CreatesContact()
    {
        var repository = new FakeGuardianContactRepository
        {
            StudentProfileExists = true
        };

        var service =
            new GuardianContactService(repository);

        var request = new CreateGuardianContactRequest
        {
            ContactType = "GUARDIAN",
            FullName = "Kamal Perera",
            Relationship = "Father",
            PhoneNumber = "0771234567",
            AlternatePhone = "0711234567",
            Email = "kamal@example.com",
            Address = "Colombo",
            IsPrimary = true
        };

        GuardianContactResponse result =
            await service.CreateAsync(1, request);

        Assert.Equal((ulong)1, result.StudentProfileId);
        Assert.Equal("GUARDIAN", result.ContactType);
        Assert.Equal("Kamal Perera", result.FullName);
        Assert.Equal("0771234567", result.PhoneNumber);
        Assert.True(result.IsPrimary);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetContacts_ReturnsContactsForStudentProfile()
    {
        var repository =
            new FakeGuardianContactRepository
            {
                StudentProfileExists = true
            };

        repository.Contacts.Add(
            CreateContact(
                1,
                1,
                "GUARDIAN",
                "Kamal Perera"));

        repository.Contacts.Add(
            CreateContact(
                2,
                1,
                "EMERGENCY",
                "Nimal Perera"));

        var service =
            new GuardianContactService(repository);

        IReadOnlyList<GuardianContactResponse> result =
            await service.GetByStudentProfileIdAsync(1);

        Assert.Equal(2, result.Count);

        Assert.Contains(
            result,
            contact =>
                contact.ContactType == "GUARDIAN");

        Assert.Contains(
            result,
            contact =>
                contact.ContactType == "EMERGENCY");
    }

    [Fact]
    public async Task GetContact_WithExistingId_ReturnsContact()
    {
        var repository =
            new FakeGuardianContactRepository();

        repository.Contacts.Add(
            CreateContact(
                10,
                2,
                "EMERGENCY",
                "Emergency Person"));

        var service =
            new GuardianContactService(repository);

        GuardianContactResponse? result =
            await service.GetByIdAsync(2, 10);

        Assert.NotNull(result);
        Assert.Equal((ulong)10, result.GuardianContactId);
        Assert.Equal("EMERGENCY", result.ContactType);
        Assert.Equal("Emergency Person", result.FullName);
    }

    [Fact]
    public async Task UpdateContact_WithValidDetails_UpdatesContact()
    {
        var repository =
            new FakeGuardianContactRepository();

        repository.Contacts.Add(
            CreateContact(
                20,
                3,
                "GUARDIAN",
                "Old Name"));

        var service =
            new GuardianContactService(repository);

        var request = new UpdateGuardianContactRequest
        {
            ContactType = "EMERGENCY",
            FullName = "Updated Name",
            Relationship = "Sibling",
            PhoneNumber = "0761234567",
            AlternatePhone = "0751234567",
            Email = "updated@example.com",
            Address = "Kandy",
            IsPrimary = true,
            IsActive = true
        };

        GuardianContactResponse? result =
            await service.UpdateAsync(
                3,
                20,
                request);

        Assert.NotNull(result);
        Assert.Equal("EMERGENCY", result.ContactType);
        Assert.Equal("Updated Name", result.FullName);
        Assert.Equal("Sibling", result.Relationship);
        Assert.Equal("0761234567", result.PhoneNumber);
        Assert.Equal("updated@example.com", result.Email);
        Assert.True(result.IsPrimary);
    }

    [Fact]
    public async Task CreateContact_WithMissingProfile_ThrowsException()
    {
        var repository =
            new FakeGuardianContactRepository
            {
                StudentProfileExists = false
            };

        var service =
            new GuardianContactService(repository);

        var request = new CreateGuardianContactRequest
        {
            ContactType = "GUARDIAN",
            FullName = "Test Guardian",
            Relationship = "Parent",
            PhoneNumber = "0771234567"
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateAsync(999, request));
    }

    private static GuardianContact CreateContact(
        ulong contactId,
        ulong studentProfileId,
        string contactType,
        string fullName)
    {
        return new GuardianContact
        {
            GuardianContactId = contactId,
            StudentProfileId = studentProfileId,
            ContactType = contactType,
            FullName = fullName,
            Relationship = "Parent",
            PhoneNumber = "0771234567",
            IsPrimary = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private sealed class FakeGuardianContactRepository
        : IGuardianContactRepository
    {
        public List<GuardianContact> Contacts { get; } = [];

        public bool StudentProfileExists { get; set; } = true;

        public Task<bool> StudentProfileExistsAsync(
            ulong studentProfileId)
        {
            return Task.FromResult(StudentProfileExists);
        }

        public Task<IReadOnlyList<GuardianContact>>
            GetByStudentProfileIdAsync(
                ulong studentProfileId)
        {
            IReadOnlyList<GuardianContact> result =
                Contacts
                    .Where(contact =>
                        contact.StudentProfileId ==
                        studentProfileId)
                    .ToList();

            return Task.FromResult(result);
        }

        public Task<GuardianContact?> GetByIdAsync(
            ulong studentProfileId,
            ulong contactId)
        {
            GuardianContact? contact =
                Contacts.FirstOrDefault(item =>
                    item.StudentProfileId ==
                    studentProfileId &&
                    item.GuardianContactId ==
                    contactId);

            return Task.FromResult(contact);
        }

        public Task<ulong> CreateAsync(
            ulong studentProfileId,
            CreateGuardianContactRequest request)
        {
            ulong newContactId =
                Contacts.Count == 0
                    ? 1
                    : Contacts.Max(contact =>
                        contact.GuardianContactId) + 1;

            Contacts.Add(new GuardianContact
            {
                GuardianContactId = newContactId,
                StudentProfileId = studentProfileId,
                ContactType =
                    request.ContactType
                        .Trim()
                        .ToUpperInvariant(),
                FullName = request.FullName.Trim(),
                Relationship =
                    request.Relationship.Trim(),
                PhoneNumber =
                    request.PhoneNumber.Trim(),
                AlternatePhone =
                    request.AlternatePhone,
                Email = request.Email,
                Address = request.Address,
                IsPrimary = request.IsPrimary,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            return Task.FromResult(newContactId);
        }

        public Task<bool> UpdateAsync(
            ulong studentProfileId,
            ulong contactId,
            UpdateGuardianContactRequest request)
        {
            GuardianContact? contact =
                Contacts.FirstOrDefault(item =>
                    item.StudentProfileId ==
                    studentProfileId &&
                    item.GuardianContactId ==
                    contactId);

            if (contact is null)
            {
                return Task.FromResult(false);
            }

            contact.ContactType =
                request.ContactType
                    .Trim()
                    .ToUpperInvariant();

            contact.FullName =
                request.FullName.Trim();

            contact.Relationship =
                request.Relationship.Trim();

            contact.PhoneNumber =
                request.PhoneNumber.Trim();

            contact.AlternatePhone =
                request.AlternatePhone;

            contact.Email = request.Email;
            contact.Address = request.Address;
            contact.IsPrimary = request.IsPrimary;
            contact.IsActive = request.IsActive;
            contact.UpdatedAt = DateTime.UtcNow;

            return Task.FromResult(true);
        }
    }
}