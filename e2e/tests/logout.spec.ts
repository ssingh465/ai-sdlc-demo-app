import { expect, test } from '@playwright/test';
import { InvoiceAppPage } from '../pages/InvoiceAppPage';

test('signing out hides the signed-in user from the topbar', async ({ page }) => {
  const invoiceApp = new InvoiceAppPage(page);
  await invoiceApp.goto();
  await invoiceApp.signIn('demo.director', 'ChangeMe123!');

  const signedInUser = page.getByText('demo.director (Director)', { exact: true });
  await expect(signedInUser).toBeVisible();

  await invoiceApp.signOut();

  await expect(signedInUser).toBeHidden();
  await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible();
});
