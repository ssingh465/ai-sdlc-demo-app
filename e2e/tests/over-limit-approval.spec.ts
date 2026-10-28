import { expect, test } from '@playwright/test';
import { InvoiceAppPage } from '../pages/InvoiceAppPage';

test("a manager's over-limit approval is refused with a reason and the invoice stays pending", async ({ page }) => {
  const invoiceApp = new InvoiceAppPage(page);
  await invoiceApp.goto();
  await invoiceApp.signIn('demo.manager', 'ChangeMe123!');

  const invoiceRow = invoiceApp.invoiceRow('INV-1007');
  await expect(invoiceRow).toBeVisible();
  const invoiceBeforeApproval = await invoiceApp.readInvoice('INV-1007');

  await invoiceApp.openApprovals();
  await invoiceApp.approve('INV-1007');

  await expect(invoiceApp.errorMessage()).toHaveText('Manager limit is 50,000; invoice is 72,500.');
  await expect(invoiceApp.pendingItem('INV-1007')).toBeVisible();
  expect(await invoiceApp.readInvoice('INV-1007')).toEqual(invoiceBeforeApproval);

  await page.reload();
  await invoiceApp.openInvoices();
  await expect(invoiceRow).toContainText('Pending Approval');
});
