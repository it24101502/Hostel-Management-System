using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Repositories;

namespace IdentityService.Services;

public class GuardianContactService : IGuardianContactService
{
    private readonly IGuardianContactRepository _repository;

    public GuardianContactService(
        IGuardianContactRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<GuardianContactResponse>>
        GetByStudentProfileIdAsync(ulong studentProfileId)
    {
        bool profileExists =
            await _repository.StudentProfileExistsAsync(
                studentProfileId);

        if (!profileExists)
        {
            throw new KeyNotFoundException(
                "Student profile was not found.");
        }

        IReadOnlyList<GuardianContact> contacts =
            await _repository.GetByStudentProfileIdAsync(
                studentProfileId);

        return contacts
            .Select(MapResponse)
            .ToList();
    }

    public async Task<GuardianContactResponse?> GetByIdAsync(
        ulong studentProfileId,
        ulong contactId)
    {
        GuardianContact? contact =
            await _repository.GetByIdAsync(
                studentProfileId,
                contactId);

        return contact is null
            ? null
            : MapResponse(contact);
    }

    public async Task<GuardianContactResponse> CreateAsync(
        ulong studentProfileId,
        CreateGuardianContactRequest request)
    {
        bool profileExists =
            await _repository.StudentProfileExistsAsync(
                studentProfileId);

        if (!profileExists)
        {
            throw new KeyNotFoundException(
                "Student profile was not found.");
        }

        request.ContactType =
            request.ContactType.Trim().ToUpperInvariant();

        ulong contactId =
            await _repository.CreateAsync(
                studentProfileId,
                request);

        GuardianContact? createdContact =
            await _repository.GetByIdAsync(
                studentProfileId,
                contactId);

        if (createdContact is null)
        {
            throw new InvalidOperationException(
                "The contact was created but could not be retrieved.");
        }

        return MapResponse(createdContact);
    }

    public async Task<GuardianContactResponse?> UpdateAsync(
        ulong studentProfileId,
        ulong contactId,
        UpdateGuardianContactRequest request)
    {
        GuardianContact? existingContact =
            await _repository.GetByIdAsync(
                studentProfileId,
                contactId);

        if (existingContact is null)
        {
            return null;
        }

        request.ContactType =
            request.ContactType.Trim().ToUpperInvariant();

        bool updated =
            await _repository.UpdateAsync(
                studentProfileId,
                contactId,
                request);

        if (!updated)
        {
            return null;
        }

        GuardianContact? updatedContact =
            await _repository.GetByIdAsync(
                studentProfileId,
                contactId);

        return updatedContact is null
            ? null
            : MapResponse(updatedContact);
    }

    private static GuardianContactResponse MapResponse(
        GuardianContact contact)
    {
        return new GuardianContactResponse
        {
            GuardianContactId =
                contact.GuardianContactId,

            StudentProfileId =
                contact.StudentProfileId,

            ContactType =
                contact.ContactType,

            FullName =
                contact.FullName,

            Relationship =
                contact.Relationship,

            PhoneNumber =
                contact.PhoneNumber,

            AlternatePhone =
                contact.AlternatePhone,

            Email =
                contact.Email,

            Address =
                contact.Address,

            IsPrimary =
                contact.IsPrimary,

            IsActive =
                contact.IsActive,

            CreatedAt =
                contact.CreatedAt,

            UpdatedAt =
                contact.UpdatedAt
        };
    }
}