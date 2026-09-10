namespace AccommodationService.Exceptions;

public class RoomCapacityExceededException : Exception
{
    public RoomCapacityExceededException()
        : base("The selected room has reached its bed capacity.")
    {
    }
}