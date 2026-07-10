import { expect, type Page } from '@playwright/test';
import type { E2ETestData } from './testData';

export async function registerThroughUi(page: Page, data: E2ETestData): Promise<void> {
  await page.goto('/');
  await page.getByRole('button', { name: 'Register' }).click();
  await page.getByLabel('Email').fill(data.email);
  await page.getByLabel('Password', { exact: true }).fill(data.password);
  await page.getByRole('button', { name: 'Create Account' }).click();

  await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
  await expect(page.getByText(data.email)).toBeVisible();
}
