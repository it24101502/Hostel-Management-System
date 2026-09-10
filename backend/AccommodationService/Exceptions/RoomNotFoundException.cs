namespace AccommodationService.Exceptions;

public class RoomNotFoundException : Exception
{
    public RoomNotFoundException()
        : base("The selected room was not found.")
    {
    }
}