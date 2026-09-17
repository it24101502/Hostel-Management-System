namespace IdentityService.Options;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } =
        "localhost:9092";

    public string StudentDeactivatedTopic { get; init; } =
        "student-deactivated";
}