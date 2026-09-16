namespace AccommodationService.Exceptions;

public class DuplicateHostelBlockException : Exception
{
    public DuplicateHostelBlockException()
        : base(
            "A hostel block with the same code or name already exists.")
    {
    }
}