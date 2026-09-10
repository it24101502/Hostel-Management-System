namespace AccommodationService.Exceptions;

public class InvalidRoomCapacityException : Exception
{
    public InvalidRoomCapacityException()
        : base("Bed capacity must be greater than zero.")
    {
    }
}
