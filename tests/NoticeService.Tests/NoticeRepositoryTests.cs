using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using NoticeService.DTOs;
using NoticeService.Repositories;

namespace NoticeService.Tests;

[Trait("Category", "MySql")]
public class NoticeRepositoryTests : IAsyncLifetime
{
    private const ulong BlockA = 9001;
    private const ulong BlockB = 9002;

    private static readonly string? ConnectionString =
        Environment.GetEnvironmentVariable(RequiresMySqlFactAttribute.VariableName);

    private readonly string _prefix = "RT-" + Guid.NewGuid().ToString("N")[..8] + " ";
    private readonly List<ulong> _ids = new();
    private NoticeRepository _repository = null!;

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString
            })
            .Build();

        _repository = new NoticeRepository(configuration);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (ConnectionString is null || _ids.Count == 0) return;

        await using var connection = new MySqlConnection(ConnectionString);
        await connection.ExecuteAsync(
            "DELETE FROM notice_audit_logs WHERE notice_id IN @ids", new { ids = _ids });
        await connection.ExecuteAsync(
            "DELETE FROM notices WHERE notice_id IN @ids", new { ids = _ids });
    }

    private async Task<NoticeResponse> CreateAsync(
        string name, ulong? block, DateOnly expiry, string type = "NOTICE")
    {
        var created = await _repository.CreateAsync(
            new CreateNoticeRequest(_prefix + name, "Body", type, block, expiry),
            10, "WARDEN");
        _ids.Add(created.NoticeId);
        return created;
    }

    private async Task<List<AuditRow>> AuditFor(ulong noticeId)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        return (await connection.QueryAsync<AuditRow>(
            @"SELECT actor_user_id AS ActorUserId, actor_role AS ActorRole,
                     action AS Action
              FROM notice_audit_logs WHERE notice_id = @noticeId
              ORDER BY audit_log_id", new { noticeId })).ToList();
    }

    private IEnumerable<string> Mine(IEnumerable<NoticeResponse> notices) =>
        notices.Where(n => n.Title.StartsWith(_prefix))
               .Select(n => n.Title[_prefix.Length..])
               .OrderBy(t => t);

    // ---------- HMS-60: CRUD ----------

    [RequiresMySqlFact]
    public async Task Create_PersistsTrimmedValuesAndMapsDateOnly()
    {
        var created = await _repository.CreateAsync(
            new CreateNoticeRequest(_prefix + "Trim", "  body  ", "SCHEDULE",
                BlockA, Today.AddDays(3)), 10, "WARDEN");
        _ids.Add(created.NoticeId);

        var loaded = await _repository.GetByIdAsync(created.NoticeId);

        Assert.NotNull(loaded);
        Assert.Equal("body", loaded.Content);
        Assert.Equal("SCHEDULE", loaded.NoticeType);
        Assert.Equal(BlockA, loaded.HostelBlockId);
        Assert.Equal(Today.AddDays(3), loaded.ExpiryDate);
        Assert.False(loaded.IsArchived);
    }

    [RequiresMySqlFact]
    public async Task Create_WritesPublishAuditRow()
    {
        var created = await CreateAsync("Audit", null, Today.AddDays(1));

        var audit = Assert.Single(await AuditFor(created.NoticeId));
        Assert.Equal("PUBLISH", audit.Action);
        Assert.Equal("WARDEN", audit.ActorRole);
        Assert.Equal((ulong)10, audit.ActorUserId);
    }

    [RequiresMySqlFact]
    public async Task Update_ChangesFieldsAndWritesAuditRow()
    {
        var created = await CreateAsync("Before", null, Today.AddDays(1));

        bool updated = await _repository.UpdateAsync(
            created.NoticeId,
            new UpdateNoticeRequest(_prefix + "After", "New body", "SCHEDULE",
                BlockB, Today.AddDays(9)),
            11, "ADMIN");

        var loaded = await _repository.GetByIdAsync(created.NoticeId);

        Assert.True(updated);
        Assert.Equal(_prefix + "After", loaded!.Title);
        Assert.Equal(BlockB, loaded.HostelBlockId);
        Assert.Equal(Today.AddDays(9), loaded.ExpiryDate);
        Assert.Contains(await AuditFor(created.NoticeId),
            a => a.Action == "UPDATE" && a.ActorRole == "ADMIN");
    }

    [RequiresMySqlFact]
    public async Task Update_MissingNotice_ReturnsFalse()
    {
        bool updated = await _repository.UpdateAsync(
            4_000_000_000_000,
            new UpdateNoticeRequest(_prefix + "x", "x", "NOTICE", null, Today.AddDays(1)),
            10, "WARDEN");

        Assert.False(updated);
    }

    [RequiresMySqlFact]
    public async Task Delete_RemovesNoticeButKeepsAuditHistory()
    {
        var created = await CreateAsync("Delete", null, Today.AddDays(1));

        bool deleted = await _repository.DeleteAsync(created.NoticeId, 10, "WARDEN");

        Assert.True(deleted);
        Assert.Null(await _repository.GetByIdAsync(created.NoticeId));
        Assert.Contains(await AuditFor(created.NoticeId), a => a.Action == "DELETE");
    }

    [RequiresMySqlFact]
    public async Task Delete_MissingNotice_ReturnsFalse() =>
        Assert.False(await _repository.DeleteAsync(4_000_000_000_000, 10, "WARDEN"));

    // ---------- HMS-61: student filtering ----------

    [RequiresMySqlFact]
    public async Task StudentNotices_ReturnOwnBlockAndGeneralOnly()
    {
        await CreateAsync("A", BlockA, Today.AddDays(2));
        await CreateAsync("B", BlockB, Today.AddDays(2));
        await CreateAsync("General", null, Today.AddDays(2));

        var visible = Mine(await _repository.GetStudentNoticesAsync(BlockA));

        Assert.Equal(new[] { "A", "General" }, visible);
    }

    [RequiresMySqlFact]
    public async Task StudentNotices_WithNoBlock_ReturnOnlyGeneral()
    {
        await CreateAsync("A", BlockA, Today.AddDays(2));
        await CreateAsync("General", null, Today.AddDays(2));

        var visible = Mine(await _repository.GetStudentNoticesAsync(0));

        Assert.Equal(new[] { "General" }, visible);
    }

    [RequiresMySqlFact]
    public async Task StudentNotices_HideExpiredAndArchived_ButShowExpiringToday()
    {
        await CreateAsync("Expired", null, Today.AddDays(-1));
        await CreateAsync("Today", null, Today);
        await CreateAsync("Future", null, Today.AddDays(1));

        Assert.Equal(new[] { "Future", "Today" },
            Mine(await _repository.GetStudentNoticesAsync(BlockA)));

        await _repository.ArchiveExpiredNoticesAsync(Today);

        Assert.Equal(new[] { "Future", "Today" },
            Mine(await _repository.GetStudentNoticesAsync(BlockA)));
    }

    // ---------- HMS-62: archiving (SQL side) ----------

    [RequiresMySqlFact]
    public async Task Archive_ArchivesOnlyExpiredAndAuditsAsSystem()
    {
        var expired = await CreateAsync("Expired", null, Today.AddDays(-2));
        var today = await CreateAsync("Today", null, Today);

        int count = await _repository.ArchiveExpiredNoticesAsync(Today);

        Assert.True(count >= 1);
        Assert.True((await _repository.GetByIdAsync(expired.NoticeId))!.IsArchived);
        Assert.False((await _repository.GetByIdAsync(today.NoticeId))!.IsArchived);

        var audit = await AuditFor(expired.NoticeId);
        Assert.Contains(audit,
            a => a.Action == "ARCHIVE" && a.ActorRole == "SYSTEM" && a.ActorUserId is null);
    }

    [RequiresMySqlFact]
    public async Task Archive_RunTwice_DoesNotAuditTwice()
    {
        var expired = await CreateAsync("Expired", null, Today.AddDays(-2));

        await _repository.ArchiveExpiredNoticesAsync(Today);
        await _repository.ArchiveExpiredNoticesAsync(Today);

        Assert.Single(await AuditFor(expired.NoticeId), a => a.Action == "ARCHIVE");
    }

    [RequiresMySqlFact]
    public async Task GetAll_ExcludesArchivedUnlessRequested()
    {
        await CreateAsync("Expired", null, Today.AddDays(-1));
        await CreateAsync("Live", null, Today.AddDays(1));
        await _repository.ArchiveExpiredNoticesAsync(Today);

        Assert.Equal(new[] { "Live" }, Mine(await _repository.GetAllAsync(false)));
        Assert.Equal(new[] { "Expired", "Live" }, Mine(await _repository.GetAllAsync(true)));
    }

    private sealed record AuditRow(ulong? ActorUserId, string ActorRole, string Action);
}