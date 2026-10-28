import { expect, test } from '@playwright/test';
import { InvoiceAppPage } from '../pages/InvoiceAppPage';

test("a manager's over-limit approval is refused with a reason and the invoice stays pending", async ({ page }) => {
  const invoiceApp = new InvoiceAppPage(page);
  await invoiceApp.goto();
  await invoiceApp.signIn('demo.manager', 'ChangeMe123!');

  const invoiceRow = page.getByRole('row').filter({ hasText: 'INV-1007' });
  const invoiceRowText = await invoiceRow.innerText();

  await invoiceApp.openApprovals();
  await invoiceApp.approve('INV-1007');

  await expect(invoiceApp.errorMessage()).toHaveText('Manager limit is 50,000; invoice is 72,500.');
  await expect(invoiceApp.pendingItem('INV-1007')).toBeVisible();

  await page.getByRole('tab', { name: 'Invoices' }).click();
  await expect(invoiceRow).toContainText('Pending Approval');
  expect(await invoiceRow.innerText()).toBe(invoiceRowText);
});
