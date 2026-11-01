namespace InvoiceApp.Domain;

public sealed record ApprovalRecord(string Approver, ApprovalRole Role, DateTime At);
