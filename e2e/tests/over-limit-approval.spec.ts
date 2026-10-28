import { expect, test } from '@playwright/test';
import { InvoiceAppPage } from '../pages/InvoiceAppPage';

test("a manager's over-limit approval is refused with a reason and the invoice stays pending", async ({ page }) => {
  const invoiceApp = new InvoiceAppPage(page);
  await invoiceApp.goto();
  await invoiceApp.signIn('demo.manager', 'ChangeMe123!');

  const invoiceRow = page.getByRole('row').filter({ hasText: 'INV-1007' });
  await expect(invoiceRow).toBeVisible();
  const readInvoice = () => page.evaluate(async () => {
    const session = JSON.parse(sessionStorage.getItem('session') ?? 'null');
    if (!session?.token) {
      throw new Error('Signed-in session token is missing.');
    }

    const response = await fetch('/api/invoices/INV-1007', {
      headers: { Authorization: `Bearer ${session.token}` },
    });
    if (!response.ok) {
      throw new Error(`Invoice request failed (${response.status}).`);
    }

    return response.json();
  });
  const invoiceBeforeApproval = await readInvoice();

  await invoiceApp.openApprovals();
  await invoiceApp.approve('INV-1007');

  await expect(invoiceApp.errorMessage()).toHaveText('Manager limit is 50,000; invoice is 72,500.');
  await expect(invoiceApp.pendingItem('INV-1007')).toBeVisible();
  expect(await readInvoice()).toEqual(invoiceBeforeApproval);

  await page.reload();
  await page.getByRole('tab', { name: 'Invoices' }).click();
  await expect(invoiceRow).toContainText('Pending Approval');
});
