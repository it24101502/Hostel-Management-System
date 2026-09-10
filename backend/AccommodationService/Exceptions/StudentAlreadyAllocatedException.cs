namespace AccommodationService.Exceptions;

public class StudentAlreadyAllocatedException : Exception
{
    public StudentAlreadyAllocatedException()
        : base("The student already has an active room allocation.")
    {
    }
}