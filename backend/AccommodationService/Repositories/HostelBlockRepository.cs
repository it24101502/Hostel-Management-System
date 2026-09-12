using AccommodationService.DTOs;
using AccommodationService.Models;
using MySqlConnector;

namespace AccommodationService.Repositories;

public class HostelBlockRepository : IHostelBlockRepository
{
    private readonly string _connectionString;

    public HostelBlockRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task<IReadOnlyList<HostelBlock>> GetActiveAsync()
    {
        const string query = """
            SELECT
                block_id,
                block_code,
                block_name,
                is_active,
                created_at,
                updated_at
            FROM hostel_blocks
            WHERE is_active = TRUE
            ORDER BY block_code;
            """;

        var blocks = new List<HostelBlock>();

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            blocks.Add(MapBlock(reader));
        }

        return blocks;
    }

    public async Task<HostelBlock?> GetByIdAsync(ulong blockId)
    {
        const string query = """
            SELECT
                block_id,
                block_code,
                block_name,
                is_active,
                created_at,
                updated_at
            FROM hostel_blocks
            WHERE block_id = @blockId
            LIMIT 1;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue("@blockId", blockId);

        await using var reader =
            await command.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? MapBlock(reader)
            : null;
    }

    public async Task<bool> DuplicateExistsAsync(
        string blockCode,
        string blockName)
    {
        const string query = """
            SELECT COUNT(*)
            FROM hostel_blocks
            WHERE block_code = @blockCode
               OR block_name = @blockName;
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@blockCode",
            blockCode.Trim());

        command.Parameters.AddWithValue(
            "@blockName",
            blockName.Trim());

        object? result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result) > 0;
    }

    public async Task<ulong> CreateAsync(
        CreateHostelBlockRequest request)
    {
        const string query = """
            INSERT INTO hostel_blocks
            (
                block_code,
                block_name,
                is_active
            )
            VALUES
            (
                @blockCode,
                @blockName,
                TRUE
            );
            """;

        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@blockCode",
            request.BlockCode.Trim().ToUpperInvariant());

        command.Parameters.AddWithValue(
            "@blockName",
            request.BlockName.Trim());

        await command.ExecuteNonQueryAsync();

        return (ulong)command.LastInsertedId;
    }

    private static HostelBlock MapBlock(
        MySqlDataReader reader)
    {
        return new HostelBlock
        {
            BlockId = reader.GetUInt64("block_id"),
            BlockCode = reader.GetString("block_code"),
            BlockName = reader.GetString("block_name"),
            IsActive = reader.GetBoolean("is_active"),
            CreatedAt = reader.GetDateTime("created_at"),
            UpdatedAt = reader.GetDateTime("updated_at")
        };
    }
}