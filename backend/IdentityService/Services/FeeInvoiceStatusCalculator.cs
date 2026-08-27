namespace IdentityService.Services;

public static class FeeInvoiceStatusCalculator
{
    public static string Determine(
        decimal totalAmount,
        decimal paidAmount,
        DateOnly dueDate,
        DateOnly today)
    {
        if (paidAmount >= totalAmount)
        {
            return "PAID";
        }

        if (dueDate < today)
        {
            return "OVERDUE";
        }

        return "UNPAID";
    }
}