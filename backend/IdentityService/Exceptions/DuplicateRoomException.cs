namespace IdentityService.Exceptions;

public class DuplicateRoomException : Exception
{
    public DuplicateRoomException()
        : base("A room with this block, floor, and room number already exists.")
    {
    }
}
