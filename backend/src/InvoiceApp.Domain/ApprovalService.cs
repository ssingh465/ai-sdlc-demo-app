using System.Globalization;

namespace InvoiceApp.Domain;

/// <summary>
/// Moves an invoice through its lifecycle: Draft -> PendingApproval -> Approved or Rejected.
/// High-value invoices require approval from two distinct users.
/// </summary>
public class ApprovalService
{
    private readonly IInvoiceRepository _repository;

    public ApprovalService(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public Invoice Submit(string invoiceId)
    {
        var invoice = Find(invoiceId);

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Invoice {invoiceId} is {invoice.Status}; only drafts can be submitted.");
        }

        invoice.Status = InvoiceStatus.PendingApproval;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Approve(string invoiceId, string approver, ApprovalRole role)
    {
        var invoice = Find(invoiceId);
        EnsurePending(invoice);

        if (role == ApprovalRole.Admin)
        {
            throw new ApprovalRefusedException("Admin has no approval authority");
        }

        var limit = ApprovalPolicy.LimitFor(role);
        if (invoice.Amount > ApprovalPolicy.TwoApproverThreshold)
        {
            if (invoice.Approvals.Count > 0)
            {
                var firstApproval = invoice.Approvals[0];
                if (string.Equals(firstApproval.Approver, approver, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ApprovalRefusedException(
                        "A second approver is required; the same user cannot approve twice.");
                }

                if (role != ApprovalRole.Director)
                {
                    throw new ApprovalRefusedException(
                        "The second approval above 500,000 must come from a Director.");
                }

                var secondApprovalAt = DateTime.UtcNow;
                invoice.Approvals.Add(new ApprovalRecord(approver, role, secondApprovalAt));
                invoice.Status = InvoiceStatus.Approved;
                invoice.DecidedBy = approver;
                invoice.DecidedAt = secondApprovalAt;
                _repository.Update(invoice);
                return invoice;
            }

            var firstApprovalAt = DateTime.UtcNow;
            invoice.Approvals.Add(new ApprovalRecord(approver, role, firstApprovalAt));
            _repository.Update(invoice);
            return invoice;
        }

        if (limit is decimal approvalLimit && invoice.Amount > approvalLimit)
        {
            var formattedLimit = approvalLimit.ToString("#,##0.##", CultureInfo.InvariantCulture);
            var formattedAmount = invoice.Amount.ToString("#,##0.##", CultureInfo.InvariantCulture);
            throw new ApprovalRefusedException(
                $"{role} limit is {formattedLimit}; invoice is {formattedAmount}.");
        }

        var approvalAt = DateTime.UtcNow;
        invoice.Approvals.Add(new ApprovalRecord(approver, role, approvalAt));
        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = approver;
        invoice.DecidedAt = approvalAt;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Reject(string invoiceId, string approver, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required to reject an invoice.", nameof(reason));
        }

        var invoice = Find(invoiceId);
        EnsurePending(invoice);

        invoice.Status = InvoiceStatus.Rejected;
        invoice.DecidedBy = approver;
        invoice.DecidedAt = DateTime.UtcNow;
        invoice.DecisionNote = reason;
        _repository.Update(invoice);
        return invoice;
    }

    private Invoice Find(string invoiceId) =>
        _repository.GetById(invoiceId)
        ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");

    private static void EnsurePending(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.PendingApproval)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} is {invoice.Status}; only pending invoices can be decided.");
        }
    }
}
