namespace LeaveService.Options;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } = "localhost:9092";

    public string LeaveEventsTopic { get; init; } = "leave-events";

    public string LeaveRequestedTopic { get; init; } = "leave-requested";
    public string LeaveApprovedTopic { get; init; } = "leave-approved";
    public string LeaveOverdueTopic { get; init; } = "leave-overdue";
}
