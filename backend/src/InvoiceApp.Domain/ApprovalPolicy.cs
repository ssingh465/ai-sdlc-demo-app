namespace InvoiceApp.Domain;

public static class ApprovalPolicy
{
    public static decimal? LimitFor(ApprovalRole role) => role switch
    {
        ApprovalRole.Manager => 50000m,
        ApprovalRole.Director => 500000m,
        ApprovalRole.Admin => null,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown approval role.")
    };
}
