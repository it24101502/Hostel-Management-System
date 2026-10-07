using System.Data;
using Dapper;
using MySqlConnector;
using NoticeService.DTOs;

namespace NoticeService.Repositories;

public class NoticeRepository : INoticeRepository
{
    private const string SelectColumns = @"
        notice_id AS NoticeId,
        title AS Title,
        content AS Content,
        notice_type AS NoticeType,
        hostel_block_id AS HostelBlockId,
        expiry_date AS ExpiryDate,
        is_archived AS IsArchived,
        archived_at AS ArchivedAt,
        created_by_user_id AS CreatedByUserId,
        created_at AS CreatedAt,
        updated_at AS UpdatedAt";

    private readonly string _connectionString;

    public NoticeRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection configuration string is missing.");
    }

    private IDbConnection CreateConnection() => new MySqlConnection(_connectionString);

    public async Task<NoticeResponse> CreateAsync(CreateNoticeRequest request, ulong userId, string userRole)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string insertNoticeSql = @"
            INSERT INTO notices (
                title, content, notice_type, hostel_block_id, expiry_date, created_by_user_id
            ) VALUES (
                @Title, @Content, UPPER(@NoticeType), @HostelBlockId, @ExpiryDate, @CreatedByUserId
            );
            SELECT LAST_INSERT_ID();";

        var noticeId = await connection.ExecuteScalarAsync<ulong>(insertNoticeSql, new
        {
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            request.NoticeType,
            request.HostelBlockId,
            request.ExpiryDate,
            CreatedByUserId = userId
        }, transaction);

        await InsertAuditAsync(
            connection, transaction, noticeId, userId, userRole, "PUBLISH", request.Title.Trim());

        transaction.Commit();

        return (await GetByIdAsync(noticeId))!;
    }

    public async Task<NoticeResponse?> GetByIdAsync(ulong noticeId)
    {
        string sql = $@"
            SELECT {SelectColumns}
            FROM notices
            WHERE notice_id = @NoticeId;";

        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<NoticeResponse>(sql, new { NoticeId = noticeId });
    }

    public async Task<IEnumerable<NoticeResponse>> GetAllAsync(bool includeArchived = false)
    {
        string sql = $"SELECT {SelectColumns} FROM notices ";

        if (!includeArchived)
        {
            sql += "WHERE is_archived = FALSE ";
        }

        sql += "ORDER BY created_at DESC;";

        using var connection = CreateConnection();
        return await connection.QueryAsync<NoticeResponse>(sql);
    }

    public async Task<bool> UpdateAsync(ulong noticeId, UpdateNoticeRequest request, ulong userId, string userRole)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string updateNoticeSql = @"
            UPDATE notices
            SET
                title = @Title,
                content = @Content,
                notice_type = UPPER(@NoticeType),
                hostel_block_id = @HostelBlockId,
                expiry_date = @ExpiryDate
            WHERE notice_id = @NoticeId;";

        var affectedRows = await connection.ExecuteAsync(updateNoticeSql, new
        {
            NoticeId = noticeId,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            request.NoticeType,
            request.HostelBlockId,
            request.ExpiryDate
        }, transaction);

        if (affectedRows == 0)
        {
            transaction.Rollback();
            return false;
        }

        await InsertAuditAsync(
            connection, transaction, noticeId, userId, userRole, "UPDATE", request.Title.Trim());

        transaction.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(ulong noticeId, ulong userId, string userRole)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string fetchNoticeSql = "SELECT title FROM notices WHERE notice_id = @NoticeId;";
        var title = await connection.ExecuteScalarAsync<string?>(
            fetchNoticeSql, new { NoticeId = noticeId }, transaction);

        if (title == null)
        {
            transaction.Rollback();
            return false;
        }

        await InsertAuditAsync(
            connection, transaction, noticeId, userId, userRole, "DELETE", title);

        const string deleteNoticeSql = "DELETE FROM notices WHERE notice_id = @NoticeId;";
        await connection.ExecuteAsync(deleteNoticeSql, new { NoticeId = noticeId }, transaction);

        transaction.Commit();
        return true;
    }

    public async Task<IEnumerable<NoticeResponse>> GetStudentNoticesAsync(ulong hostelBlockId)
    {
        string sql = $@"
            SELECT {SelectColumns}
            FROM notices
            WHERE is_archived = FALSE
              AND expiry_date >= UTC_DATE()
              AND (hostel_block_id = @HostelBlockId OR hostel_block_id IS NULL)
            ORDER BY created_at DESC;";

        using var connection = CreateConnection();
        return await connection.QueryAsync<NoticeResponse>(sql, new { HostelBlockId = hostelBlockId });
    }

    public async Task<int> ArchiveExpiredNoticesAsync(DateOnly currentDate)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        // Pick the notices first so the audit rows cover exactly
        // the notices archived by this run, and no others.
        const string selectSql = @"
            SELECT notice_id AS NoticeId, title AS Title
            FROM notices
            WHERE is_archived = FALSE
              AND expiry_date < @CurrentDate
            FOR UPDATE;";

        var expired = (await connection.QueryAsync<ExpiredNotice>(
            selectSql, new { CurrentDate = currentDate }, transaction)).ToList();

        if (expired.Count == 0)
        {
            transaction.Rollback();
            return 0;
        }

        const string archiveSql = @"
            UPDATE notices
            SET is_archived = TRUE,
                archived_at = UTC_TIMESTAMP(6)
            WHERE is_archived = FALSE
              AND notice_id IN @Ids;";

        int archivedCount = await connection.ExecuteAsync(
            archiveSql,
            new { Ids = expired.Select(n => n.NoticeId).ToArray() },
            transaction);

        // The system acts here, so actor_user_id is NULL and the
        // role is 'SYSTEM' (the only value the CHECK allows for it).
        const string auditSql = @"
            INSERT INTO notice_audit_logs (
                notice_id, actor_user_id, actor_role, action, title
            ) VALUES (
                @NoticeId, NULL, 'SYSTEM', 'ARCHIVE', @Title
            );";

        await connection.ExecuteAsync(auditSql, expired, transaction);

        transaction.Commit();
        return archivedCount;
    }

    private static Task InsertAuditAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        ulong noticeId,
        ulong userId,
        string userRole,
        string action,
        string title)
    {
        const string sql = @"
            INSERT INTO notice_audit_logs (
                notice_id, actor_user_id, actor_role, action, title
            ) VALUES (
                @NoticeId, @ActorUserId, UPPER(@ActorRole), @Action, @Title
            );";

        return connection.ExecuteAsync(sql, new
        {
            NoticeId = noticeId,
            ActorUserId = userId,
            ActorRole = userRole,
            Action = action,
            Title = title
        }, transaction);
    }

    private sealed record ExpiredNotice(ulong NoticeId, string Title);
}