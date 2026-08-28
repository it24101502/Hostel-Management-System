using IdentityService.Models;
using IdentityService.Repositories;
using IdentityService.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentityService.Tests;

public class OverdueFeeJobServiceTests
{
    [Fact]
    public async Task
        BeforeDueDate_DoesNotMarkInvoiceOverdue()
    {
        DateTimeOffset simulatedNow =
            new(
                2026,
                8,
                27,
                10,
                0,
                0,
                TimeSpan.Zero);

        var repository =
            new FakeOverdueFeeRepository
            {
                DueDate =
                    new DateOnly(
                        2026,
                        8,
                        30)
            };

        var sender =
            new FakeReminderSender();

        var service =
            CreateService(
                repository,
                sender,
                simulatedNow);

        OverdueFeeJobResult result =
            await service.RunOnceAsync();

        Assert.Equal(
            0,
            result.InvoicesMarkedOverdue);

        Assert.Equal(
            0,
            result.RemindersSent);

        Assert.Equal(
            "UNPAID",
            repository.InvoiceStatus);
    }

    [Fact]
    public async Task
        OnDueDate_DoesNotMarkInvoiceOverdue()
    {
        DateTimeOffset simulatedNow =
            new(
                2026,
                8,
                30,
                10,
                0,
                0,
                TimeSpan.Zero);

        var repository =
            new FakeOverdueFeeRepository
            {
                DueDate =
                    new DateOnly(
                        2026,
                        8,
                        30)
            };

        var service =
            CreateService(
                repository,
                new FakeReminderSender(),
                simulatedNow);

        OverdueFeeJobResult result =
            await service.RunOnceAsync();

        Assert.Equal(
            0,
            result.InvoicesMarkedOverdue);

        Assert.Equal(
            "UNPAID",
            repository.InvoiceStatus);
    }

    [Fact]
    public async Task
        AfterDueDate_MarksOverdueAndSendsReminder()
    {
        DateTimeOffset simulatedNow =
            new(
                2026,
                8,
                31,
                10,
                0,
                0,
                TimeSpan.Zero);

        var repository =
            new FakeOverdueFeeRepository
            {
                DueDate =
                    new DateOnly(
                        2026,
                        8,
                        30)
            };

        var sender =
            new FakeReminderSender();

        var service =
            CreateService(
                repository,
                sender,
                simulatedNow);

        OverdueFeeJobResult result =
            await service.RunOnceAsync();

        Assert.Equal(
            1,
            result.InvoicesMarkedOverdue);

        Assert.Equal(
            1,
            result.RemindersSent);

        Assert.Equal(
            0,
            result.RemindersFailed);

        Assert.Equal(
            "OVERDUE",
            repository.InvoiceStatus);

        Assert.Equal(
            "SENT",
            repository.ReminderStatus);

        Assert.Equal(
            1,
            sender.SendCount);
    }

    [Fact]
    public async Task
        JobUsesDateFromMockedTimeProvider()
    {
        DateTimeOffset simulatedNow =
            new(
                2030,
                5,
                15,
                12,
                0,
                0,
                TimeSpan.Zero);

        var repository =
            new FakeOverdueFeeRepository
            {
                DueDate =
                    new DateOnly(
                        2030,
                        5,
                        14)
            };

        var service =
            CreateService(
                repository,
                new FakeReminderSender(),
                simulatedNow);

        OverdueFeeJobResult result =
            await service.RunOnceAsync();

        Assert.Equal(
            new DateOnly(
                2030,
                5,
                15),
            result.ProcessingDate);

        Assert.Equal(
            result.ProcessingDate,
            repository.LastProcessingDate);

        Assert.Equal(
            1,
            result.InvoicesMarkedOverdue);
    }

    [Fact]
    public async Task
        ReminderFailure_IsRecordedAsFailed()
    {
        DateTimeOffset simulatedNow =
            new(
                2026,
                9,
                1,
                10,
                0,
                0,
                TimeSpan.Zero);

        var repository =
            new FakeOverdueFeeRepository
            {
                DueDate =
                    new DateOnly(
                        2026,
                        8,
                        30)
            };

        var sender =
            new FakeReminderSender
            {
                ThrowWhenSending = true
            };

        var service =
            CreateService(
                repository,
                sender,
                simulatedNow);

        OverdueFeeJobResult result =
            await service.RunOnceAsync();

        Assert.Equal(
            1,
            result.InvoicesMarkedOverdue);

        Assert.Equal(
            0,
            result.RemindersSent);

        Assert.Equal(
            1,
            result.RemindersFailed);

        Assert.Equal(
            "FAILED",
            repository.ReminderStatus);

        Assert.Contains(
            "Simulated reminder failure",
            repository.FailureReason);
    }

    [Fact]
    public async Task
        RunningJobTwice_DoesNotSendDuplicateReminder()
    {
        DateTimeOffset simulatedNow =
            new(
                2026,
                9,
                1,
                10,
                0,
                0,
                TimeSpan.Zero);

        var repository =
            new FakeOverdueFeeRepository
            {
                DueDate =
                    new DateOnly(
                        2026,
                        8,
                        30)
            };

        var sender =
            new FakeReminderSender();

        var service =
            CreateService(
                repository,
                sender,
                simulatedNow);

        OverdueFeeJobResult firstResult =
            await service.RunOnceAsync();

        OverdueFeeJobResult secondResult =
            await service.RunOnceAsync();

        Assert.Equal(
            1,
            firstResult.InvoicesMarkedOverdue);

        Assert.Equal(
            0,
            secondResult.InvoicesMarkedOverdue);

        Assert.Equal(
            1,
            sender.SendCount);
    }

    private static OverdueFeeJobService
        CreateService(
            FakeOverdueFeeRepository repository,
            FakeReminderSender sender,
            DateTimeOffset simulatedNow)
    {
        return new OverdueFeeJobService(
            repository,
            sender,
            new FixedTimeProvider(
                simulatedNow),
            NullLogger<
                OverdueFeeJobService>.Instance);
    }

    private sealed class FixedTimeProvider
        : TimeProvider
    {
        private readonly DateTimeOffset
            _utcNow;

        public FixedTimeProvider(
            DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset
            GetUtcNow()
        {
            return _utcNow;
        }
    }

    private sealed class FakeReminderSender
        : IFeeReminderSender
    {
        public bool ThrowWhenSending
        {
            get;
            set;
        }

        public int SendCount
        {
            get;
            private set;
        }

        public Task SendAsync(
            FeeReminderNotification reminder,
            CancellationToken cancellationToken =
                default)
        {
            SendCount++;

            if (ThrowWhenSending)
            {
                throw new InvalidOperationException(
                    "Simulated reminder failure.");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class
        FakeOverdueFeeRepository
        : IOverdueFeeRepository
    {
        public DateOnly DueDate
        {
            get;
            set;
        }

        public decimal TotalAmount
        {
            get;
            set;
        } = 25000m;

        public decimal PaidAmount
        {
            get;
            set;
        } = 0m;

        public string InvoiceStatus
        {
            get;
            private set;
        } = "UNPAID";

        public string ReminderStatus
        {
            get;
            private set;
        } = "";

        public string FailureReason
        {
            get;
            private set;
        } = "";

        public DateOnly LastProcessingDate
        {
            get;
            private set;
        }

        private FeeReminderNotification?
            _reminder;

        public Task<int>
            MarkOverdueAndCreateRemindersAsync(
                DateOnly processingDate,
                CancellationToken cancellationToken =
                    default)
        {
            LastProcessingDate =
                processingDate;

            bool shouldMarkOverdue =
                InvoiceStatus == "UNPAID" &&
                DueDate < processingDate &&
                PaidAmount < TotalAmount;

            if (!shouldMarkOverdue)
            {
                return Task.FromResult(0);
            }

            InvoiceStatus = "OVERDUE";

            if (_reminder is null)
            {
                _reminder =
                    new FeeReminderNotification
                    {
                        ReminderId = 100,
                        InvoiceId = 1,
                        StudentProfileId = 10,
                        RecipientUserId = 20,

                        InvoiceNumber =
                            "INV-TEST-001",

                        TotalAmount =
                            TotalAmount,

                        PaidAmount =
                            PaidAmount,

                        DueDate =
                            DueDate,

                        Message =
                            "Your fee invoice is overdue.",

                        NotificationStatus =
                            "PENDING",

                        TriggeredAt =
                            processingDate
                                .ToDateTime(
                                    TimeOnly.MinValue)
                    };

                ReminderStatus = "PENDING";
            }

            return Task.FromResult(1);
        }

        public Task<
            IReadOnlyList<
                FeeReminderNotification>>
            GetPendingRemindersAsync(
                CancellationToken cancellationToken =
                    default)
        {
            IReadOnlyList<
                FeeReminderNotification> result =
                    _reminder is not null &&
                    ReminderStatus == "PENDING"
                        ? new[] { _reminder }
                        : Array.Empty<
                            FeeReminderNotification>();

            return Task.FromResult(result);
        }

        public Task MarkReminderSentAsync(
            ulong reminderId,
            DateTime sentAt,
            CancellationToken cancellationToken =
                default)
        {
            ReminderStatus = "SENT";

            if (_reminder is not null)
            {
                _reminder.NotificationStatus =
                    "SENT";
            }

            return Task.CompletedTask;
        }

        public Task MarkReminderFailedAsync(
            ulong reminderId,
            string failureReason,
            CancellationToken cancellationToken =
                default)
        {
            ReminderStatus = "FAILED";
            FailureReason = failureReason;

            if (_reminder is not null)
            {
                _reminder.NotificationStatus =
                    "FAILED";
            }

            return Task.CompletedTask;
        }
    }
}