using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

internal sealed record InvoiceSnapshot(
    string Id,
    string CustomerName,
    InvoiceStatus Status,
    decimal Amount,
    DateTime CreatedAt,
    string? DecidedBy,
    DateTime? DecidedAt,
    string? DecisionNote,
    IReadOnlyList<(string Approver, ApprovalRole Role, DateTime At)> Approvals)
{
    public static InvoiceSnapshot Of(Invoice invoice) =>
        new(invoice.Id, invoice.CustomerName, invoice.Status, invoice.Amount, invoice.CreatedAt,
            invoice.DecidedBy, invoice.DecidedAt, invoice.DecisionNote,
            invoice.Approvals.Select(approval => (approval.Approver, approval.Role, approval.At)).ToArray());

    public static void AssertUnchanged(InvoiceSnapshot before, Invoice? invoiceAfter)
    {
        Assert.NotNull(invoiceAfter);
        var after = Of(invoiceAfter);
        Assert.Equal(before.Id, after.Id);
        Assert.Equal(before.CustomerName, after.CustomerName);
        Assert.Equal(before.Status, after.Status);
        Assert.Equal(before.Amount, after.Amount);
        Assert.Equal(before.CreatedAt, after.CreatedAt);
        Assert.Equal(before.DecidedBy, after.DecidedBy);
        Assert.Equal(before.DecidedAt, after.DecidedAt);
        Assert.Equal(before.DecisionNote, after.DecisionNote);
        Assert.Equal(before.Approvals, after.Approvals);
    }
}
