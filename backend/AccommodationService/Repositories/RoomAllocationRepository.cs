using AccommodationService.DTOs;
using AccommodationService.Exceptions;
using AccommodationService.Models;
using MySqlConnector;

namespace AccommodationService.Repositories;

public class RoomAllocationRepository : IRoomAllocationRepository
{
    private readonly string _connectionString;

    public RoomAllocationRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task<IReadOnlyList<StudentRoomAllocation>> GetAllAsync()
    {
        const string query = """
            SELECT
                sra.allocation_id,
                sra.student_profile_id,
                sra.room_id,
                hr.block_id,
                hb.block_code,
                hb.block_name,
                hr.floor_number,
                hr.room_number,
                hr.bed_capacity,
                hr.is_active,
                (
                    SELECT COUNT(*)
                    FROM student_room_allocations AS occupied
                    WHERE occupied.room_id = sra.room_id
                ) AS current_occupancy,
                sra.allocated_at,
                sra.updated_at
            FROM student_room_allocations AS sra
            INNER JOIN hostel_rooms AS hr
                ON hr.room_id = sra.room_id
            INNER JOIN hostel_blocks AS hb
                ON hb.block_id = hr.block_id
            ORDER BY
                hb.block_code,
                hr.floor_number,
                hr.room_number,
                sra.student_profile_id;
            """;

        var allocations = new List<StudentRoomAllocation>();

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            allocations.Add(MapAllocation(reader));
        }

        return allocations;
    }

    public async Task<StudentRoomAllocation?> GetByStudentIdAsync(
        ulong studentProfileId)
    {
        const string query = """
            SELECT
                sra.allocation_id,
                sra.student_profile_id,
                sra.room_id,
                hr.block_id,
                hb.block_code,
                hb.block_name,
                hr.floor_number,
                hr.room_number,
                hr.bed_capacity,
                hr.is_active,
                (
                    SELECT COUNT(*)
                    FROM student_room_allocations AS occupied
                    WHERE occupied.room_id = sra.room_id
                ) AS current_occupancy,
                sra.allocated_at,
                sra.updated_at
            FROM student_room_allocations AS sra
            INNER JOIN hostel_rooms AS hr
                ON hr.room_id = sra.room_id
            INNER JOIN hostel_blocks AS hb
                ON hb.block_id = hr.block_id
            WHERE sra.student_profile_id = @studentProfileId
            LIMIT 1;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@studentProfileId",
            studentProfileId);

        await using var reader =
            await command.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? MapAllocation(reader)
            : null;
    }

    public async Task<StudentRoomAllocation> AllocateAsync(
        AllocateStudentRequest request,
        ulong administratorUserId)
    {
        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            RoomState room =
                await GetRoomForUpdateAsync(
                    connection,
                    transaction,
                    request.RoomId)
                ?? throw new RoomNotFoundException();

            if (!room.IsActive)
            {
                throw new InactiveRoomException();
            }

            if (await StudentHasAllocationForUpdateAsync(
                    connection,
                    transaction,
                    request.StudentProfileId))
            {
                throw new StudentAlreadyAllocatedException();
            }

            int currentOccupancy =
                await GetRoomOccupancyAsync(
                    connection,
                    transaction,
                    request.RoomId);

            if (currentOccupancy >= room.BedCapacity)
            {
                throw new RoomCapacityExceededException();
            }

            ulong allocationId =
                await InsertAllocationAsync(
                    connection,
                    transaction,
                    request.StudentProfileId,
                    request.RoomId);

            await InsertAuditAsync(
                connection,
                transaction,
                allocationId,
                administratorUserId,
                request.StudentProfileId,
                "ALLOCATE",
                null,
                request.RoomId);

            await transaction.CommitAsync();
        }
        catch (MySqlException exception)
            when (exception.Number == 1062)
        {
            await transaction.RollbackAsync();
            throw new StudentAlreadyAllocatedException();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return await GetByStudentIdAsync(request.StudentProfileId)
            ?? throw new InvalidOperationException(
                "The created allocation could not be loaded.");
    }

    public async Task<StudentRoomAllocation> TransferAsync(
        ulong studentProfileId,
        TransferStudentRequest request,
        ulong administratorUserId)
    {
        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            AllocationState allocation =
                await GetAllocationForUpdateAsync(
                    connection,
                    transaction,
                    studentProfileId)
                ?? throw new StudentAllocationNotFoundException();

            if (allocation.RoomId == request.NewRoomId)
            {
                throw new SameRoomTransferException();
            }

            RoomState targetRoom =
                await GetRoomForUpdateAsync(
                    connection,
                    transaction,
                    request.NewRoomId)
                ?? throw new RoomNotFoundException();

            if (!targetRoom.IsActive)
            {
                throw new InactiveRoomException();
            }

            int targetOccupancy =
                await GetRoomOccupancyAsync(
                    connection,
                    transaction,
                    request.NewRoomId);

            if (targetOccupancy >= targetRoom.BedCapacity)
            {
                throw new RoomCapacityExceededException();
            }

            await UpdateAllocationRoomAsync(
                connection,
                transaction,
                allocation.AllocationId,
                request.NewRoomId);

            await InsertAuditAsync(
                connection,
                transaction,
                allocation.AllocationId,
                administratorUserId,
                studentProfileId,
                "TRANSFER",
                allocation.RoomId,
                request.NewRoomId);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return await GetByStudentIdAsync(studentProfileId)
            ?? throw new InvalidOperationException(
                "The transferred allocation could not be loaded.");
    }

    public async Task<bool> ReleaseAsync(
        ulong studentProfileId,
        ulong administratorUserId)
    {
        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            AllocationState? allocation =
                await GetAllocationForUpdateAsync(
                    connection,
                    transaction,
                    studentProfileId);

            if (allocation is null)
            {
                await transaction.CommitAsync();
                return false;
            }

            await InsertAuditAsync(
                connection,
                transaction,
                allocation.AllocationId,
                administratorUserId,
                studentProfileId,
                "RELEASE",
                allocation.RoomId,
                null);

            await DeleteAllocationAsync(
                connection,
                transaction,
                allocation.AllocationId);

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<RoomOccupancyResponse>>
        GetOccupancyReportAsync(
            ulong? blockId,
            ushort? floorNumber)
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
                COUNT(sra.allocation_id) AS current_occupancy,
                GREATEST(
                    CAST(hr.bed_capacity AS SIGNED) -
                    COUNT(sra.allocation_id),
                    0
                ) AS available_beds,
                CASE
                    WHEN hr.is_active = FALSE THEN 'INACTIVE'
                    WHEN COUNT(sra.allocation_id) >= hr.bed_capacity
                        THEN 'FULL'
                    ELSE 'AVAILABLE'
                END AS room_status,
                hr.is_active
            FROM hostel_rooms AS hr
            INNER JOIN hostel_blocks AS hb
                ON hb.block_id = hr.block_id
            LEFT JOIN student_room_allocations AS sra
                ON sra.room_id = hr.room_id
            WHERE (@blockId IS NULL OR hr.block_id = @blockId)
              AND (@floorNumber IS NULL OR hr.floor_number = @floorNumber)
            GROUP BY
                hr.room_id,
                hr.block_id,
                hb.block_code,
                hb.block_name,
                hr.floor_number,
                hr.room_number,
                hr.bed_capacity,
                hr.is_active
            ORDER BY
                hb.block_code,
                hr.floor_number,
                hr.room_number;
            """;

        var report = new List<RoomOccupancyResponse>();

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@blockId",
            (object?)blockId ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@floorNumber",
            (object?)floorNumber ?? DBNull.Value);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            report.Add(new RoomOccupancyResponse
            {
                RoomId = Convert.ToUInt64(reader["room_id"]),
                BlockId = Convert.ToUInt64(reader["block_id"]),
                BlockCode = Convert.ToString(reader["block_code"])!,
                BlockName = Convert.ToString(reader["block_name"])!,
                FloorNumber = Convert.ToUInt16(reader["floor_number"]),
                RoomNumber = Convert.ToString(reader["room_number"])!,
                BedCapacity = Convert.ToUInt16(reader["bed_capacity"]),
                CurrentOccupancy =
                    Convert.ToInt32(reader["current_occupancy"]),
                AvailableBeds =
                    Convert.ToInt32(reader["available_beds"]),
                Status = Convert.ToString(reader["room_status"])!,
                IsActive = Convert.ToBoolean(reader["is_active"])
            });
        }

        return report;
    }

    private static async Task<RoomState?> GetRoomForUpdateAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        ulong roomId)
    {
        const string query = """
            SELECT bed_capacity, is_active
            FROM hostel_rooms
            WHERE room_id = @roomId
            FOR UPDATE;
            """;

        await using var command =
            new MySqlCommand(query, connection, transaction);

        command.Parameters.AddWithValue("@roomId", roomId);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new RoomState(
            Convert.ToUInt16(reader["bed_capacity"]),
            Convert.ToBoolean(reader["is_active"]));
    }

    private static async Task<bool>
        StudentHasAllocationForUpdateAsync(
            MySqlConnection connection,
            MySqlTransaction transaction,
            ulong studentProfileId)
    {
        const string query = """
            SELECT allocation_id
            FROM student_room_allocations
            WHERE student_profile_id = @studentProfileId
            LIMIT 1
            FOR UPDATE;
            """;

        await using var command =
            new MySqlCommand(query, connection, transaction);

        command.Parameters.AddWithValue(
            "@studentProfileId",
            studentProfileId);

        return await command.ExecuteScalarAsync() is not null;
    }

    private static async Task<int> GetRoomOccupancyAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        ulong roomId)
    {
        const string query = """
            SELECT COUNT(*)
            FROM student_room_allocations
            WHERE room_id = @roomId;
            """;

        await using var command =
            new MySqlCommand(query, connection, transaction);

        command.Parameters.AddWithValue("@roomId", roomId);

        object? result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result);
    }

    private static async Task<ulong> InsertAllocationAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        ulong studentProfileId,
        ulong roomId)
    {
        const string query = """
            INSERT INTO student_room_allocations
                (student_profile_id, room_id)
            VALUES
                (@studentProfileId, @roomId);
            """;

        await using var command =
            new MySqlCommand(query, connection, transaction);

        command.Parameters.AddWithValue(
            "@studentProfileId",
            studentProfileId);
        command.Parameters.AddWithValue("@roomId", roomId);

        await command.ExecuteNonQueryAsync();

        return checked((ulong)command.LastInsertedId);
    }

    private static async Task<AllocationState?>
        GetAllocationForUpdateAsync(
            MySqlConnection connection,
            MySqlTransaction transaction,
            ulong studentProfileId)
    {
        const string query = """
            SELECT allocation_id, room_id
            FROM student_room_allocations
            WHERE student_profile_id = @studentProfileId
            LIMIT 1
            FOR UPDATE;
            """;

        await using var command =
            new MySqlCommand(query, connection, transaction);

        command.Parameters.AddWithValue(
            "@studentProfileId",
            studentProfileId);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new AllocationState(
            Convert.ToUInt64(reader["allocation_id"]),
            Convert.ToUInt64(reader["room_id"]));
    }

    private static async Task UpdateAllocationRoomAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        ulong allocationId,
        ulong roomId)
    {
        const string query = """
            UPDATE student_room_allocations
            SET
                room_id = @roomId,
                updated_at = CURRENT_TIMESTAMP
            WHERE allocation_id = @allocationId;
            """;

        await using var command =
            new MySqlCommand(query, connection, transaction);

        command.Parameters.AddWithValue(
            "@allocationId",
            allocationId);
        command.Parameters.AddWithValue("@roomId", roomId);

        await command.ExecuteNonQueryAsync();
    }

    private static async Task DeleteAllocationAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        ulong allocationId)
    {
        const string query = """
            DELETE FROM student_room_allocations
            WHERE allocation_id = @allocationId;
            """;

        await using var command =
            new MySqlCommand(
                query,
                connection,
                transaction);

        command.Parameters.AddWithValue(
            "@allocationId",
            allocationId);

        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertAuditAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        ulong allocationId,
        ulong administratorUserId,
        ulong studentProfileId,
        string action,
        ulong? fromRoomId,
        ulong? toRoomId)
    {
        const string query = """
            INSERT INTO student_room_allocation_audit_logs
            (
                allocation_id,
                administrator_user_id,
                student_profile_id,
                action,
                from_room_id,
                to_room_id
            )
            VALUES
            (
                @allocationId,
                @administratorUserId,
                @studentProfileId,
                @action,
                @fromRoomId,
                @toRoomId
            );
            """;

        await using var command =
            new MySqlCommand(query, connection, transaction);

        command.Parameters.AddWithValue(
            "@allocationId",
            allocationId);
        command.Parameters.AddWithValue(
            "@administratorUserId",
            administratorUserId);
        command.Parameters.AddWithValue(
            "@studentProfileId",
            studentProfileId);
        command.Parameters.AddWithValue("@action", action);
        command.Parameters.AddWithValue(
            "@fromRoomId",
            (object?)fromRoomId ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "@toRoomId",
            (object?)toRoomId ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }

    private static StudentRoomAllocation MapAllocation(
        MySqlDataReader reader)
    {
        return new StudentRoomAllocation
        {
            AllocationId =
                Convert.ToUInt64(reader["allocation_id"]),
            StudentProfileId =
                Convert.ToUInt64(reader["student_profile_id"]),
            RoomId = Convert.ToUInt64(reader["room_id"]),
            BlockId = Convert.ToUInt64(reader["block_id"]),
            BlockCode = Convert.ToString(reader["block_code"])!,
            BlockName = Convert.ToString(reader["block_name"])!,
            FloorNumber =
                Convert.ToUInt16(reader["floor_number"]),
            RoomNumber = Convert.ToString(reader["room_number"])!,
            BedCapacity =
                Convert.ToUInt16(reader["bed_capacity"]),
            IsActive = Convert.ToBoolean(reader["is_active"]),
            CurrentOccupancy =
                Convert.ToInt32(reader["current_occupancy"]),
            AllocatedAt =
                Convert.ToDateTime(reader["allocated_at"]),
            UpdatedAt =
                Convert.ToDateTime(reader["updated_at"])
        };
    }

    private sealed record RoomState(
        ushort BedCapacity,
        bool IsActive);

    private sealed record AllocationState(
        ulong AllocationId,
        ulong RoomId);
}
