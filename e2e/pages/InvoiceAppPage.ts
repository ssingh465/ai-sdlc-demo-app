import type { Page } from '@playwright/test';

export class InvoiceAppPage {
  constructor(private readonly page: Page) {}

  async goto(): Promise<void> {
    await this.page.goto('/');
  }

  async signIn(username: string, password: string): Promise<void> {
    await this.page.getByLabel('Username').fill(username);
    await this.page.getByLabel('Password').fill(password);
    await this.page.getByRole('button', { name: 'Sign in' }).click();
  }

  async openApprovals(): Promise<void> {
    await this.page.getByRole('tab', { name: 'Approvals' }).click();
  }

  async openInvoices(): Promise<void> {
    await this.page.getByRole('tab', { name: 'Invoices' }).click();
  }

  async approve(invoiceId: string): Promise<void> {
    await this.page.getByRole('button', { name: `Approve invoice ${invoiceId}` }).click();
  }

  invoiceRow(invoiceId: string) {
    return this.page.getByRole('row').filter({ hasText: invoiceId });
  }

  async readInvoice(invoiceId: string): Promise<unknown> {
    return this.page.evaluate(async (id) => {
      const session = JSON.parse(sessionStorage.getItem('session') ?? 'null');
      if (!session?.token) {
        throw new Error('Signed-in session token is missing.');
      }

      const response = await fetch(`/api/invoices/${id}`, {
        headers: { Authorization: 'Bearer ' + session.token },
      });
      if (!response.ok) {
        throw new Error(`Invoice request failed (${response.status}).`);
      }

      return response.json();
    }, invoiceId);
  }

  errorMessage() {
    return this.page.getByRole('tabpanel', { name: 'Approvals' }).getByRole('alert');
  }

  pendingItem(invoiceId: string) {
    return this.page.getByRole('listitem').filter({ hasText: invoiceId });
  }
}
