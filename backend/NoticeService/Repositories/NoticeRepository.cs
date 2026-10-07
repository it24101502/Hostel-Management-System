using System.Data;
using Dapper;
using MySqlConnector;
using NoticeService.DTOs;

namespace NoticeService.Repositories;

public class NoticeRepository : INoticeRepository
{
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

        const string insertAuditSql = @"
            INSERT INTO notice_audit_logs (
                notice_id, actor_user_id, actor_role, action, title
            ) VALUES (
                @NoticeId, @ActorUserId, UPPER(@ActorRole), 'PUBLISH', @Title
            );";

        await connection.ExecuteAsync(insertAuditSql, new
        {
            NoticeId = noticeId,
            ActorUserId = userId,
            ActorRole = userRole,
            Title = request.Title.Trim()
        }, transaction);

        transaction.Commit();

        return (await GetByIdAsync(noticeId))!;
    }

    public async Task<NoticeResponse?> GetByIdAsync(ulong noticeId)
    {
        const string sql = @"
            SELECT
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
                updated_at AS UpdatedAt
            FROM notices
            WHERE notice_id = @NoticeId;";

        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<NoticeResponse>(sql, new { NoticeId = noticeId });
    }

    public async Task<IEnumerable<NoticeResponse>> GetAllAsync(bool includeArchived = false)
    {
        string sql = @"
            SELECT
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
                updated_at AS UpdatedAt
            FROM notices ";

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
            return false;
        }

        const string insertAuditSql = @"
            INSERT INTO notice_audit_logs (
                notice_id, actor_user_id, actor_role, action, title
            ) VALUES (
                @NoticeId, @ActorUserId, UPPER(@ActorRole), 'UPDATE', @Title
            );";

        await connection.ExecuteAsync(insertAuditSql, new
        {
            NoticeId = noticeId,
            ActorUserId = userId,
            ActorRole = userRole,
            Title = request.Title.Trim()
        }, transaction);

        transaction.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(ulong noticeId, ulong userId, string userRole)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string fetchNoticeSql = "SELECT title FROM notices WHERE notice_id = @NoticeId;";
        var title = await connection.ExecuteScalarAsync<string?>(fetchNoticeSql, new { NoticeId = noticeId }, transaction);

        if (title == null)
        {
            return false;
        }

        const string insertAuditSql = @"
            INSERT INTO notice_audit_logs (
                notice_id, actor_user_id, actor_role, action, title
            ) VALUES (
                @NoticeId, @ActorUserId, UPPER(@ActorRole), 'DELETE', @Title
            );";

        await connection.ExecuteAsync(insertAuditSql, new
        {
            NoticeId = noticeId,
            ActorUserId = userId,
            ActorRole = userRole,
            Title = title
        }, transaction);

        const string deleteNoticeSql = "DELETE FROM notices WHERE notice_id = @NoticeId;";
        await connection.ExecuteAsync(deleteNoticeSql, new { NoticeId = noticeId }, transaction);

        transaction.Commit();
        return true;
    }

    public async Task<IEnumerable<NoticeResponse>> GetStudentNoticesAsync(ulong hostelBlockId)
    {
        const string sql = @"
            SELECT 
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
                updated_at AS UpdatedAt
            FROM notices
            WHERE is_archived = FALSE 
              AND expiry_date >= CURRENT_DATE()
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

        const string archiveSql = @"
            UPDATE notices
            SET 
                is_archived = TRUE,
                archived_at = CURRENT_TIMESTAMP()
            WHERE is_archived = FALSE
              AND expiry_date < @CurrentDate;";

        var archivedCount = await connection.ExecuteAsync(archiveSql, new { CurrentDate = currentDate }, transaction);

        if (archivedCount > 0)
        {
            const string auditSql = @"
                INSERT INTO notice_audit_logs (
                    notice_id, actor_user_id, actor_role, action, title
                )
                SELECT 
                    notice_id, 0, 'SYSTEM_JOB', 'ARCHIVE', CONCAT('Auto-archived on expiry date check: ', title)
                FROM notices
                WHERE is_archived = TRUE 
                  AND DATE(archived_at) = CURRENT_DATE();";

            await connection.ExecuteAsync(auditSql, transaction: transaction);
        }

        transaction.Commit();
        return archivedCount;
    }
}