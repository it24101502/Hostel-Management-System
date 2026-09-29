namespace LeaveService.Exceptions;

public class LeaveRequestNotFoundException : Exception
{
    public LeaveRequestNotFoundException()
        : base("The leave request was not found.")
    {
    }
}
