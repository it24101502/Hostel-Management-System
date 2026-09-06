using IdentityService.Models;
using MySqlConnector;

namespace IdentityService.Repositories;

public class RoomAuditRepository : IRoomAuditRepository
{
    private readonly string _connectionString;

    public RoomAuditRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString(
                "DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task RecordAsync(
        ulong administratorUserId,
        string action,
        HostelRoom room)
    {
        const string sql = """
            INSERT INTO room_audit_logs
            (
                administrator_user_id,
                room_id,
                action,
                block_id,
                floor_number,
                room_number,
                bed_capacity,
                occurred_at
            )
            VALUES
            (
                @administratorUserId,
                @roomId,
                @action,
                @blockId,
                @floorNumber,
                @roomNumber,
                @bedCapacity,
                UTC_TIMESTAMP(6)
            );
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@administratorUserId",
            administratorUserId);

        command.Parameters.AddWithValue(
            "@roomId",
            room.RoomId);

        command.Parameters.AddWithValue(
            "@action",
            action);

        command.Parameters.AddWithValue(
            "@blockId",
            room.BlockId);

        command.Parameters.AddWithValue(
            "@floorNumber",
            room.FloorNumber);

        command.Parameters.AddWithValue(
            "@roomNumber",
            room.RoomNumber);

        command.Parameters.AddWithValue(
            "@bedCapacity",
            room.BedCapacity);

        await command.ExecuteNonQueryAsync();
    }
}