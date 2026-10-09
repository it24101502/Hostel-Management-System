using MySqlConnector;

namespace IdentityService.Repositories;

public class StudentBlockRepository : IStudentBlockRepository
{
    private readonly string _connectionString;

    public StudentBlockRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    public async Task ApplyAllocationChangeAsync(
        ulong studentProfileId,
        ulong? blockId,
        string? blockCode,
        string? blockName,
        CancellationToken cancellationToken = default)
    {
        if (blockId.HasValue &&
            (string.IsNullOrWhiteSpace(blockCode) ||
             string.IsNullOrWhiteSpace(blockName)))
        {
            throw new ArgumentException(
                "Block code and name are required when a block is set.");
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (blockId.HasValue)
            {
                // Keep the block ID identical to AccommodationService so
                // the FK on student_profiles stays valid.
                const string upsert = """
                    INSERT INTO hostel_blocks
                        (block_id, block_code, block_name, is_active)
                    VALUES
                        (@blockId, @blockCode, @blockName, TRUE) AS incoming
                    ON DUPLICATE KEY UPDATE
                        block_code = incoming.block_code,
                        block_name = incoming.block_name,
                        is_active  = TRUE;
                    """;

                await using var command =
                    new MySqlCommand(upsert, connection, transaction);
                command.Parameters.AddWithValue("@blockId", blockId.Value);
                command.Parameters.AddWithValue("@blockCode", blockCode!.Trim());
                command.Parameters.AddWithValue("@blockName", blockName!.Trim());
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            const string update = """
                UPDATE student_profiles
                SET hostel_block_id = @blockId
                WHERE student_profile_id = @studentProfileId;
                """;

            await using (var command =
                new MySqlCommand(update, connection, transaction))
            {
                command.Parameters.AddWithValue(
                    "@blockId", (object?)blockId ?? DBNull.Value);
                command.Parameters.AddWithValue(
                    "@studentProfileId", studentProfileId);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}