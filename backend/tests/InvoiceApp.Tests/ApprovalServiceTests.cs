using System.Globalization;
using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceTests
{
    private static Invoice InvoiceWith(string id, InvoiceStatus status, decimal amount = 100m) =>
        new()
        {
            Id = id,
            CustomerName = "Test School",
            Amount = amount,
            Status = status,
            CreatedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            DecidedBy = "previous.approver",
            DecidedAt = new DateTime(2026, 1, 3, 3, 4, 5, DateTimeKind.Utc),
            DecisionNote = "previous decision"
        };

    private static (ApprovalService Service, FakeInvoiceRepository Repository) ServiceFor(params Invoice[] invoices)
    {
        var repository = new FakeInvoiceRepository(invoices);
        return (new ApprovalService(repository), repository);
    }

    private sealed record InvoiceSnapshot(
        string Id,
        string CustomerName,
        decimal Amount,
        InvoiceStatus Status,
        DateTime CreatedAt,
        string? DecidedBy,
        DateTime? DecidedAt,
        string? DecisionNote);

    private static InvoiceSnapshot Snapshot(Invoice invoice) =>
        new(invoice.Id, invoice.CustomerName, invoice.Amount, invoice.Status, invoice.CreatedAt,
            invoice.DecidedBy, invoice.DecidedAt, invoice.DecisionNote);

    private static void AssertUnchanged(InvoiceSnapshot before, Invoice? after)
    {
        Assert.NotNull(after);
        Assert.Equal(before, Snapshot(after));
    }

    [Fact]
    public void Submit_MovesDraftToPendingApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft);

        var result = ServiceFor(invoice).Service.Submit("INV-1");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
    }

    [Fact]
    public void Submit_RejectsInvoiceThatIsNotADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Approved)).Service;

        var exception = Assert.Throws<InvalidOperationException>(() => service.Submit("INV-1"));

        Assert.Equal("Invoice INV-1 is Approved; only drafts can be submitted.", exception.Message);
    }

    [Fact]
    public void Approve_SetsStatusApproverAndTimestamp()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        var previousDecisionTime = invoice.DecidedAt;
        var service = ServiceFor(invoice).Service;

        var result = service.Approve("INV-1", "demo.manager", ApprovalRole.Manager);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.manager", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        Assert.NotEqual(previousDecisionTime, result.DecidedAt);
    }

    [Fact]
    public void Approve_RejectsInvoiceThatIsStillADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Draft)).Service;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.Approve("INV-1", "demo.manager", ApprovalRole.Manager));

        Assert.Equal("Invoice INV-1 is Draft; only pending invoices can be decided.", exception.Message);
    }

    [Fact]
    public void Approve_ThrowsWhenInvoiceDoesNotExist()
    {
        var service = ServiceFor().Service;

        var exception = Assert.Throws<KeyNotFoundException>(() =>
            service.Approve("INV-404", "demo.manager", ApprovalRole.Manager));

        Assert.Equal("Invoice INV-404 was not found.", exception.Message);
    }

    [Fact]
    public void Reject_RecordsReasonAndApprover()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval)).Service;

        var result = service.Reject("INV-1", "demo.manager", "Duplicate invoice");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("demo.manager", result.DecidedBy);
        Assert.Equal("Duplicate invoice", result.DecisionNote);
    }

    [Fact]
    public void Reject_RequiresAReason()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval)).Service;

        var exception = Assert.Throws<ArgumentException>(
            () => service.Reject("INV-1", "demo.manager", "  "));

        Assert.Equal("A reason is required to reject an invoice. (Parameter 'reason')", exception.Message);
    }

    [Fact]
    public void Manager_CanApprove_AtExactly50000()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 50000m);
        var (service, repository) = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.manager", ApprovalRole.Manager);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal(1, repository.UpdateCalls);
    }

    [Fact]
    public void Manager_IsRefused_At50000_01_AndInvoiceUnchanged()
    {
        AssertRefusedWithoutChanges(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 50000.01m),
            ApprovalRole.Manager,
            "demo.manager",
            "Manager limit is 50,000; invoice is 50,000.01.");
    }

    [Fact]
    public void Director_CanApprove_72500()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 72500m);
        var (service, repository) = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.director", ApprovalRole.Director);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal(1, repository.UpdateCalls);
    }

    [Fact]
    public void Director_CanApprove_AtExactly500000()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500000m);
        var (service, repository) = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.director", ApprovalRole.Director);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal(1, repository.UpdateCalls);
    }

    [Fact]
    public void Director_IsRefused_At500000_01_AndInvoiceUnchanged()
    {
        AssertRefusedWithoutChanges(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500000.01m),
            ApprovalRole.Director,
            "demo.director",
            "Director limit is 500,000; invoice is 500,000.01.");
    }

    [Fact]
    public void Admin_IsRefused_WithNoApprovalAuthority_AndInvoiceUnchanged()
    {
        AssertRefusedWithoutChanges(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 72500m),
            ApprovalRole.Admin,
            "demo.admin",
            "Admin has no approval authority");
    }

    [Fact]
    public void RefusalMessage_ContainsRoleLimitAndAmount()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var exception = AssertRefusedWithoutChanges(
                InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 72500m),
                ApprovalRole.Manager,
                "demo.manager",
                "Manager limit is 50,000; invoice is 72,500.");

            Assert.Equal("Manager limit is 50,000; invoice is 72,500.", exception.Message);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Reject_HasNoAmountLimit()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 650000m);
        var (service, repository) = ServiceFor(invoice);

        var result = service.Reject("INV-1", "demo.manager", "Reviewed and declined");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("Reviewed and declined", result.DecisionNote);
        Assert.Equal(1, repository.UpdateCalls);
    }

    private static ApprovalRefusedException AssertRefusedWithoutChanges(
        Invoice invoice,
        ApprovalRole role,
        string approver,
        string expectedMessage)
    {
        var before = Snapshot(invoice);
        var (service, repository) = ServiceFor(invoice);

        var exception = Assert.Throws<ApprovalRefusedException>(
            () => service.Approve(invoice.Id, approver, role));

        Assert.Equal(expectedMessage, exception.Message);
        AssertUnchanged(before, repository.GetById(invoice.Id));
        Assert.Equal(0, repository.UpdateCalls);
        return exception;
    }
}
