using System.Globalization;
using LeaveService.DTOs;
using LeaveService.Events;
using LeaveService.Exceptions;
using LeaveService.Models;
using LeaveService.Repositories;

namespace LeaveService.Services;

public class LeaveRequestService : ILeaveRequestService
{
    public const int MaxReasonLength = 500;

    public const int MaxCompanionNameLength = 200;

    public const int MaxCompanionRelationshipLength = 100;

    public const int MaxCompanionPhoneLength = 20;

    private const int MinPhoneDigits = 7;

    private const int MaxPhoneDigits = 15;

    private readonly ILeaveRequestRepository _repository;
    private readonly ILeaveEventPublisher _eventPublisher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LeaveRequestService> _logger;

    public LeaveRequestService(
        ILeaveRequestRepository repository,
        ILeaveEventPublisher eventPublisher,
        TimeProvider timeProvider,
        ILogger<LeaveRequestService> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<LeaveRequestResponse> SubmitAsync(
        SubmitLeaveRequest request,
        ulong studentUserId,
        string studentUsername)
    {
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        DateOnly today =
            DateOnly.FromDateTime(utcNow.UtcDateTime);

        var errors = Validate(request, today);

        if (errors.Count > 0)
        {
            throw new LeaveRequestValidationException(
                errors.ToDictionary(
                    error => error.Key,
                    error => error.Value.ToArray()));
        }

        // Validate guarantees these values are present.
        var newRequest = new NewLeaveRequest
        {
            StudentUserId = studentUserId,
            StudentUsername = studentUsername,
            DepartureDate = request.DepartureDate!.Value,
            ExpectedReturnDate = request.ExpectedReturnDate!.Value,
            Reason = request.Reason!.Trim(),
            CompanionName = request.CompanionName!.Trim(),
            CompanionRelationship =
                request.CompanionRelationship!.Trim(),
            CompanionPhone = request.CompanionPhone!.Trim()
        };

        var notification = new NewLeaveNotification
        {
            NotificationType = LeaveNotificationTypes.LeaveSubmitted,
            Message =
                $"{studentUsername} submitted a leave request " +
                $"for {FormatDate(newRequest.DepartureDate)} to " +
                $"{FormatDate(newRequest.ExpectedReturnDate)}."
        };

        LeaveRequest created =
            await _repository.CreateWithNotificationAsync(
                newRequest,
                notification,
                utcNow.UtcDateTime);

        await PublishSubmittedEventAsync(created, utcNow);

        return LeaveRequestMapper.ToResponse(created);
    }

    public async Task<IReadOnlyList<LeaveRequestResponse>>
        GetMyRequestsAsync(ulong studentUserId)
    {
        var requests =
            await _repository.GetByStudentAsync(studentUserId);

        return requests
            .Select(LeaveRequestMapper.ToResponse)
            .ToList();
    }

    public async Task<LeaveRequestResponse?> GetMyRequestAsync(
        ulong leaveRequestId,
        ulong studentUserId)
    {
        var request =
            await _repository.GetByIdAsync(leaveRequestId);

        // Another student's request is reported as "not found"
        // so its existence is not revealed.
        if (request is null ||
            request.StudentUserId != studentUserId)
        {
            return null;
        }

        return LeaveRequestMapper.ToResponse(request);
    }

    private static Dictionary<string, List<string>> Validate(
        SubmitLeaveRequest request,
        DateOnly today)
    {
        var errors = new Dictionary<string, List<string>>();

        if (request.DepartureDate is null)
        {
            AddError(
                errors,
                "departureDate",
                "Departure date is required.");
        }
        else if (request.DepartureDate.Value < today)
        {
            AddError(
                errors,
                "departureDate",
                "Departure date cannot be in the past.");
        }

        if (request.ExpectedReturnDate is null)
        {
            AddError(
                errors,
                "expectedReturnDate",
                "Expected return date is required.");
        }
        else if (request.DepartureDate is not null &&
                 request.ExpectedReturnDate.Value <
                 request.DepartureDate.Value)
        {
            AddError(
                errors,
                "expectedReturnDate",
                "Expected return date cannot be earlier than the departure date.");
        }

        ValidateText(
            errors,
            "reason",
            "Reason",
            request.Reason,
            MaxReasonLength);

        ValidateText(
            errors,
            "companionName",
            "Companion or guardian name",
            request.CompanionName,
            MaxCompanionNameLength);

        ValidateText(
            errors,
            "companionRelationship",
            "Relationship to the student",
            request.CompanionRelationship,
            MaxCompanionRelationshipLength);

        ValidatePhone(errors, request.CompanionPhone);

        return errors;
    }

    private static void ValidateText(
        Dictionary<string, List<string>> errors,
        string fieldName,
        string label,
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddError(
                errors,
                fieldName,
                $"{label} is required.");

            return;
        }

        if (value.Trim().Length > maxLength)
        {
            AddError(
                errors,
                fieldName,
                $"{label} cannot exceed {maxLength} characters.");
        }
    }

    private static void ValidatePhone(
        Dictionary<string, List<string>> errors,
        string? phone)
    {
        const string fieldName = "companionPhone";

        if (string.IsNullOrWhiteSpace(phone))
        {
            AddError(
                errors,
                fieldName,
                "Companion or guardian phone number is required.");

            return;
        }

        string trimmed = phone.Trim();

        bool onlyAllowedCharacters = trimmed.All(
            character =>
                char.IsAsciiDigit(character) ||
                character is '+' or '-' or '(' or ')' or ' ');

        int digitCount = trimmed.Count(char.IsAsciiDigit);

        if (trimmed.Length > MaxCompanionPhoneLength ||
            !onlyAllowedCharacters ||
            digitCount < MinPhoneDigits ||
            digitCount > MaxPhoneDigits)
        {
            AddError(
                errors,
                fieldName,
                "Enter a valid phone number using digits, spaces, +, - and brackets.");
        }
    }

    private static void AddError(
        Dictionary<string, List<string>> errors,
        string fieldName,
        string message)
    {
        if (!errors.TryGetValue(fieldName, out var messages))
        {
            messages = new List<string>();
            errors[fieldName] = messages;
        }

        messages.Add(message);
    }

    private static string FormatDate(DateOnly date)
    {
        return date.ToString(
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);
    }

    private async Task PublishSubmittedEventAsync(
        LeaveRequest request,
        DateTimeOffset occurredAtUtc)
    {
        try
        {
            await _eventPublisher.PublishAsync(
                new LeaveEvent(
                    Guid.NewGuid(),
                    LeaveEventTypes.RequestSubmitted,
                    request.LeaveRequestId,
                    request.StudentUserId,
                    request.Status,
                    request.StudentUserId,
                    occurredAtUtc));
        }
        catch (Exception exception)
        {
            // The request is already saved. A messaging failure
            // must not turn a successful submission into an error.
            _logger.LogWarning(
                exception,
                "Unable to publish the submission event for leave request {LeaveRequestId}.",
                request.LeaveRequestId);
        }
    }
}
