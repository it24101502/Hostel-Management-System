using LeaveService.Models;
using MySqlConnector;

namespace LeaveService.Repositories;

public class LeaveNotificationRepository
    : ILeaveNotificationRepository
{
    private readonly string _connectionString;

    public LeaveNotificationRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task<IReadOnlyList<LeaveNotification>>
        GetForStaffAsync(bool unreadOnly)
    {
        const string query = """
            SELECT
                notification_id,
                leave_request_id,
                audience,
                notification_type,
                message,
                is_read,
                read_by_user_id,
                read_at,
                created_at
            FROM leave_notifications
            WHERE audience = 'STAFF'
              AND (@unreadOnly = FALSE OR is_read = FALSE)
            ORDER BY created_at DESC, notification_id DESC
            LIMIT 100;
            """;

        var notifications = new List<LeaveNotification>();

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@unreadOnly",
            unreadOnly);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            notifications.Add(MapNotification(reader));
        }

        return notifications;
    }

    public async Task<bool> MarkReadAsync(
        ulong notificationId,
        ulong staffUserId,
        DateTime readAtUtc)
    {
        const string updateQuery = """
            UPDATE leave_notifications
            SET
                is_read = TRUE,
                read_by_user_id = @staffUserId,
                read_at = @readAt
            WHERE notification_id = @notificationId
              AND is_read = FALSE;
            """;

        const string existsQuery = """
            SELECT 1
            FROM leave_notifications
            WHERE notification_id = @notificationId
            LIMIT 1;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using (var update =
            new MySqlCommand(updateQuery, connection))
        {
            update.Parameters.AddWithValue(
                "@staffUserId",
                staffUserId);

            update.Parameters.AddWithValue(
                "@readAt",
                readAtUtc);

            update.Parameters.AddWithValue(
                "@notificationId",
                notificationId);

            await update.ExecuteNonQueryAsync();
        }

        await using var exists =
            new MySqlCommand(existsQuery, connection);

        exists.Parameters.AddWithValue(
            "@notificationId",
            notificationId);

        return await exists.ExecuteScalarAsync() is not null;
    }

    private static LeaveNotification MapNotification(
        MySqlDataReader reader)
    {
        return new LeaveNotification
        {
            NotificationId =
                Convert.ToUInt64(reader["notification_id"]),

            LeaveRequestId =
                Convert.ToUInt64(reader["leave_request_id"]),

            Audience = Convert.ToString(reader["audience"])!,

            NotificationType =
                Convert.ToString(reader["notification_type"])!,

            Message = Convert.ToString(reader["message"])!,

            IsRead = Convert.ToBoolean(reader["is_read"]),

            ReadByUserId =
                reader["read_by_user_id"] is DBNull
                    ? null
                    : Convert.ToUInt64(reader["read_by_user_id"]),

            ReadAt =
                reader["read_at"] is DBNull
                    ? null
                    : DateTime.SpecifyKind(
                        reader.GetDateTime("read_at"),
                        DateTimeKind.Utc),

            CreatedAt =
                DateTime.SpecifyKind(
                    reader.GetDateTime("created_at"),
                    DateTimeKind.Utc)
        };
    }
}
