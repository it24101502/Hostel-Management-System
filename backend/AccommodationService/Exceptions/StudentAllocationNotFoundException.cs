namespace AccommodationService.Exceptions;

public class StudentAllocationNotFoundException : Exception
{
    public StudentAllocationNotFoundException()
        : base("The student's active room allocation was not found.")
    {
    }
}