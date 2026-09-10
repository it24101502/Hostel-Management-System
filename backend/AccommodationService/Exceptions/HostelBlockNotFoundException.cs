namespace AccommodationService.Exceptions;

public class HostelBlockNotFoundException : Exception
{
    public HostelBlockNotFoundException()
        : base("The selected hostel block was not found or is inactive.")
    {
    }
}

