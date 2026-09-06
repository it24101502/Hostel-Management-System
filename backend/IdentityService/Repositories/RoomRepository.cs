using IdentityService.DTOs;
using IdentityService.Models;
using MySqlConnector;

namespace IdentityService.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly string _connectionString;

    public RoomRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task<IReadOnlyList<HostelRoom>> GetAllAsync()
    {
        const string query = """
            SELECT
                hr.room_id,
                hr.block_id,
                hb.block_code,
                hb.block_name,
                hr.floor_number,
                hr.room_number,
                hr.bed_capacity,
                hr.is_active,
                hr.created_at,
                hr.updated_at
            FROM hostel_rooms AS hr
            INNER JOIN hostel_blocks AS hb
                ON hb.block_id = hr.block_id
            ORDER BY
                hb.block_code,
                hr.floor_number,
                hr.room_number;
            """;

        var rooms = new List<HostelRoom>();

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            rooms.Add(MapRoom(reader));
        }

        return rooms;
    }

    public async Task<HostelRoom?> GetByIdAsync(ulong roomId)
    {
        const string query = """
            SELECT
                hr.room_id,
                hr.block_id,
                hb.block_code,
                hb.block_name,
                hr.floor_number,
                hr.room_number,
                hr.bed_capacity,
                hr.is_active,
                hr.created_at,
                hr.updated_at
            FROM hostel_rooms AS hr
            INNER JOIN hostel_blocks AS hb
                ON hb.block_id = hr.block_id
            WHERE hr.room_id = @roomId
            LIMIT 1;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue("@roomId", roomId);

        await using var reader =
            await command.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? MapRoom(reader)
            : null;
    }

    public async Task<bool> BlockExistsAsync(ulong blockId)
    {
        const string query = """
            SELECT COUNT(*)
            FROM hostel_blocks
            WHERE block_id = @blockId
              AND is_active = TRUE;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue("@blockId", blockId);

        object? result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result) > 0;
    }

    public async Task<bool> LocationExistsAsync(
        ulong blockId,
        ushort floorNumber,
        string roomNumber,
        ulong? excludedRoomId = null)
    {
        const string query = """
            SELECT COUNT(*)
            FROM hostel_rooms
            WHERE block_id = @blockId
              AND floor_number = @floorNumber
              AND room_number = @roomNumber
              AND
              (
                  @excludedRoomId IS NULL
                  OR room_id <> @excludedRoomId
              );
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue("@blockId", blockId);
        command.Parameters.AddWithValue("@floorNumber", floorNumber);
        command.Parameters.AddWithValue(
            "@roomNumber",
            roomNumber.Trim());
        command.Parameters.AddWithValue(
            "@excludedRoomId",
            excludedRoomId.HasValue
                ? excludedRoomId.Value
                : DBNull.Value);

        object? result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result) > 0;
    }

    public async Task<ulong> CreateAsync(
        CreateRoomRequest request)
    {
        const string query = """
            INSERT INTO hostel_rooms
            (
                block_id,
                floor_number,
                room_number,
                bed_capacity,
                is_active
            )
            VALUES
            (
                @blockId,
                @floorNumber,
                @roomNumber,
                @bedCapacity,
                TRUE
            );
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue("@blockId", request.BlockId);
        command.Parameters.AddWithValue(
            "@floorNumber",
            request.FloorNumber);
        command.Parameters.AddWithValue(
            "@roomNumber",
            request.RoomNumber.Trim());
        command.Parameters.AddWithValue(
            "@bedCapacity",
            request.BedCapacity);

        await command.ExecuteNonQueryAsync();

        return (ulong)command.LastInsertedId;
    }

    public async Task<bool> UpdateAsync(
        ulong roomId,
        UpdateRoomRequest request)
    {
        const string query = """
            UPDATE hostel_rooms
            SET
                block_id = @blockId,
                floor_number = @floorNumber,
                room_number = @roomNumber,
                bed_capacity = @bedCapacity
            WHERE room_id = @roomId;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue("@blockId", request.BlockId);
        command.Parameters.AddWithValue(
            "@floorNumber",
            request.FloorNumber);
        command.Parameters.AddWithValue(
            "@roomNumber",
            request.RoomNumber.Trim());
        command.Parameters.AddWithValue(
            "@bedCapacity",
            request.BedCapacity);
        command.Parameters.AddWithValue("@roomId", roomId);

        int affectedRows = await command.ExecuteNonQueryAsync();

        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(ulong roomId)
    {
        const string query = """
            DELETE FROM hostel_rooms
            WHERE room_id = @roomId;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue("@roomId", roomId);

        int affectedRows = await command.ExecuteNonQueryAsync();

        return affectedRows > 0;
    }

    private static HostelRoom MapRoom(MySqlDataReader reader)
    {
        return new HostelRoom
        {
            RoomId = reader.GetUInt64("room_id"),
            BlockId = reader.GetUInt64("block_id"),
            BlockCode = reader.GetString("block_code"),
            BlockName = reader.GetString("block_name"),
            FloorNumber = reader.GetUInt16("floor_number"),
            RoomNumber = reader.GetString("room_number"),
            BedCapacity = reader.GetUInt16("bed_capacity"),
            IsActive = reader.GetBoolean("is_active"),
            CreatedAt = reader.GetDateTime("created_at"),
            UpdatedAt = reader.GetDateTime("updated_at")
        };
    }
}
