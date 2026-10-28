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

    [Fact]
    public void Submit_MovesDraftToPendingApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft);
        var (service, repository) = ServiceFor(invoice);

        var result = service.Submit("INV-1");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        Assert.Equal(1, repository.UpdateCalls);
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
    public void Reject_ApprovedInvoice_ThrowsAndLeavesInvoiceUnchanged()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Approved);
        invoice.Approvals.Add(new ApprovalRecord(
            "demo.director",
            ApprovalRole.Director,
            new DateTime(2026, 1, 4, 3, 4, 5, DateTimeKind.Utc)));
        var before = InvoiceSnapshot.Of(invoice);
        var (service, repository) = ServiceFor(invoice);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Reject(invoice.Id, "demo.manager", "No longer needed"));

        Assert.Equal("Invoice INV-1 is Approved; only pending invoices can be decided.", exception.Message);
        InvoiceSnapshot.AssertUnchanged(before, repository.GetById(invoice.Id));
        Assert.Equal(0, repository.UpdateCalls);
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
    public void Above500000_At500000_01_DirectorApproval_IsFirstOnly_StaysPending()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500000.01m);
        invoice.DecidedBy = null;
        invoice.DecidedAt = null;
        var (service, repository) = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.director", ApprovalRole.Director);

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        Assert.Null(result.DecidedBy);
        Assert.Null(result.DecidedAt);
        var approval = Assert.Single(result.Approvals);
        Assert.Equal("demo.director", approval.Approver);
        Assert.Equal(ApprovalRole.Director, approval.Role);
        Assert.NotEqual(default, approval.At);
        Assert.Equal(1, repository.UpdateCalls);
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
    public void Above500000_FirstApprovalByManager_StaysPendingWithOneApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 650000m);
        invoice.DecidedBy = null;
        invoice.DecidedAt = null;
        var (service, repository) = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.manager", ApprovalRole.Manager);

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        Assert.Null(result.DecidedBy);
        Assert.Null(result.DecidedAt);
        var approval = Assert.Single(result.Approvals);
        Assert.Equal("demo.manager", approval.Approver);
        Assert.Equal(ApprovalRole.Manager, approval.Role);
        Assert.NotEqual(default, approval.At);
        Assert.Equal(1, repository.UpdateCalls);
    }

    [Fact]
    public void Above500000_FirstApprovalByDirector_StaysPendingWithOneApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 650000m);
        invoice.DecidedBy = null;
        invoice.DecidedAt = null;
        var (service, repository) = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.director", ApprovalRole.Director);

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        Assert.Null(result.DecidedBy);
        Assert.Null(result.DecidedAt);
        var approval = Assert.Single(result.Approvals);
        Assert.Equal("demo.director", approval.Approver);
        Assert.Equal(ApprovalRole.Director, approval.Role);
        Assert.NotEqual(default, approval.At);
        Assert.Equal(1, repository.UpdateCalls);
    }

    [Fact]
    public void Above500000_SecondApprovalByDifferentDirector_Approves()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 650000m);
        invoice.DecidedBy = null;
        invoice.DecidedAt = null;
        var (service, repository) = ServiceFor(invoice);
        service.Approve("INV-1", "demo.manager", ApprovalRole.Manager);

        var result = service.Approve("INV-1", "demo.director", ApprovalRole.Director);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        Assert.Collection(result.Approvals,
            first => Assert.Equal(("demo.manager", ApprovalRole.Manager), (first.Approver, first.Role)),
            second => Assert.Equal(("demo.director", ApprovalRole.Director), (second.Approver, second.Role)));
        Assert.Equal(2, repository.UpdateCalls);
    }

    [Fact]
    public void Above500000_SecondApprovalByManager_IsRefused_AndInvoiceUnchanged()
    {
        AssertRefusedAfterFirstApproval(
            "demo.director",
            ApprovalRole.Director,
            "demo.manager",
            ApprovalRole.Manager,
            "The second approval above 500,000 must come from a Director.");
    }

    [Fact]
    public void Above500000_SameDirectorTwice_IsRefused_AndInvoiceUnchanged()
    {
        AssertRefusedAfterFirstApproval(
            "demo.director",
            ApprovalRole.Director,
            "demo.director",
            ApprovalRole.Director,
            "A second approver is required; the same user cannot approve twice.");
    }

    [Fact]
    public void Above500000_SameUserDifferentCasing_IsRefused_AndInvoiceUnchanged()
    {
        AssertRefusedAfterFirstApproval(
            "demo.director",
            ApprovalRole.Director,
            "Demo.Director",
            ApprovalRole.Director,
            "A second approver is required; the same user cannot approve twice.");
    }

    [Fact]
    public void Above500000_AdminAsSecond_IsRefused_AndInvoiceUnchanged()
    {
        AssertRefusedAfterFirstApproval(
            "demo.director",
            ApprovalRole.Director,
            "demo.admin",
            ApprovalRole.Admin,
            "Admin has no approval authority");
    }

    [Fact]
    public void Above500000_ManagerFirstThenSameManagerAgain_IsRefused_AndInvoiceUnchanged()
    {
        AssertRefusedAfterFirstApproval(
            "demo.manager",
            ApprovalRole.Manager,
            "DEMO.MANAGER",
            ApprovalRole.Manager,
            "A second approver is required; the same user cannot approve twice.");
    }

    [Fact]
    public void AtOrBelow500000_Approval_RecordsExactlyOneApprovalRecord()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500000m);
        var service = ServiceFor(invoice).Service;

        var result = service.Approve("INV-1", "demo.director", ApprovalRole.Director);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        var approval = Assert.Single(result.Approvals);
        Assert.Equal("demo.director", approval.Approver);
        Assert.Equal(ApprovalRole.Director, approval.Role);
        Assert.NotEqual(default, approval.At);
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
        var before = InvoiceSnapshot.Of(invoice);
        var (service, repository) = ServiceFor(invoice);

        var exception = Assert.Throws<ApprovalRefusedException>(
            () => service.Approve(invoice.Id, approver, role));

        Assert.Equal(expectedMessage, exception.Message);
        InvoiceSnapshot.AssertUnchanged(before, repository.GetById(invoice.Id));
        Assert.Equal(0, repository.UpdateCalls);
        return exception;
    }

    private static void AssertRefusedAfterFirstApproval(
        string firstApprover,
        ApprovalRole firstRole,
        string secondApprover,
        ApprovalRole secondRole,
        string expectedMessage)
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 650000m);
        invoice.DecidedBy = null;
        invoice.DecidedAt = null;
        var (service, repository) = ServiceFor(invoice);
        service.Approve(invoice.Id, firstApprover, firstRole);
        var before = InvoiceSnapshot.Of(invoice);

        var exception = Assert.Throws<ApprovalRefusedException>(
            () => service.Approve(invoice.Id, secondApprover, secondRole));

        Assert.Equal(expectedMessage, exception.Message);
        InvoiceSnapshot.AssertUnchanged(before, repository.GetById(invoice.Id));
        Assert.Equal(1, repository.UpdateCalls);
    }
}
