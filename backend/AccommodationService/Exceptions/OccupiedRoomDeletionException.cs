namespace AccommodationService.Exceptions;

public class OccupiedRoomDeletionException : Exception
{
    public OccupiedRoomDeletionException()
        : base("The room cannot be deleted because it has active occupants.")
    {
    }
}
