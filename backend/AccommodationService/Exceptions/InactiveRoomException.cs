namespace AccommodationService.Exceptions;

public class InactiveRoomException : Exception
{
    public InactiveRoomException()
        : base("The selected room is inactive and cannot receive students.")
    {
    }
}