namespace AccommodationService.Exceptions;

public class RoomCapacityBelowOccupancyException : Exception
{
    public RoomCapacityBelowOccupancyException(int occupancy)
        : base(
            "Bed capacity cannot be lower than the room's current " +
            $"occupancy ({occupancy}). Transfer students out first.")
    {
    }
}