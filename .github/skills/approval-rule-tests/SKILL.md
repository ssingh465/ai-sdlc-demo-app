---
name: approval-rule-tests
description: "Write xUnit approval-rule tests with FakeInvoiceRepository, boundary cases, exact refusal reasons, and complete invoice snapshots."
---

# Approval Rule Tests

Use for approval-rule changes in `backend/tests/InvoiceApp.Tests`.

- Add tests to the class for the service under test and use `FakeInvoiceRepository` to seed invoices.
- Name tests `Rule_Condition_Outcome`, for example `ManagerLimit_AmountAboveLimit_ReturnsRefusal`.
- Cover both sides of each boundary: 50,000 and 50,000.01; 500,000 and 500,000.01.
- For each refusal, assert the exact reason message. Snapshot every invoice property before the service call and assert that every property is unchanged afterward, including recorded approvals.
- Deep-copy collection properties in snapshots. Keep the snapshot in sync when invoice properties change.

```csharp
var before = JsonSerializer.Serialize(invoice);

var exception = Assert.Throws<ApprovalRefusedException>(
    () => service.Approve(invoice.Id, username, role));

Assert.Equal("Manager limit is 50,000; invoice is 72,500.", exception.Message);
var invoiceAfter = repository.GetById(invoice.Id);
Assert.Equal(before, JsonSerializer.Serialize(invoiceAfter));
```

Ensure serialization covers every property and recorded approvals; do not omit a field just because it was not changed by the refusal.

## Checklist

- Would this test fail if the rule it names were deleted?
