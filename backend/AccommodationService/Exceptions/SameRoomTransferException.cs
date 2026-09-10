namespace AccommodationService.Exceptions;

public class SameRoomTransferException : Exception
{
    public SameRoomTransferException()
        : base("The student is already allocated to the selected room.")
    {
    }
}