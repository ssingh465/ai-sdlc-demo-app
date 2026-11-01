namespace InvoiceApp.Domain;

public class ApprovalRefusedException : InvalidOperationException
{
    public ApprovalRefusedException(string reason) : base(reason)
    {
    }
}
