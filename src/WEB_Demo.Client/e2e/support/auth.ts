import { expect, type Page } from '@playwright/test';
import type { E2ETestData } from './testData';

export async function useEnglish(page: Page): Promise<void> {
  const language = page.getByRole('combobox', { name: /介面語言|Interface language/ });
  await language.click();
  await page.getByRole('option', { name: 'English', exact: true }).click();
}

export async function registerThroughUi(page: Page, data: E2ETestData): Promise<void> {
  await page.goto('/');
  await useEnglish(page);
  await page.getByRole('button', { name: 'Register' }).click();
  await page.getByLabel('Email').fill(data.email);
  await page.getByLabel('Password', { exact: true }).fill(data.password);
  await page.getByRole('button', { name: 'Create Account' }).click();

  await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
  await expect(page.getByText(data.email)).toBeVisible();
}

export async function loginThroughUi(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/');
  await useEnglish(page);
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Sign In' }).click();
  await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
}
