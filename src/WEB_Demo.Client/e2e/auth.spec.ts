import { expect, test } from '@playwright/test';
import { registerThroughUi } from './support/auth';
import { createE2ETestData } from './support/testData';

test('使用者可以註冊、登出並重新登入', async ({ page }) => {
  const data = createE2ETestData();

  await registerThroughUi(page, data);
  await page.getByRole('button', { name: 'Logout' }).click();

  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
  await page.getByLabel('Email').fill(data.email);
  await page.getByLabel('Password', { exact: true }).fill(data.password);
  await page.getByRole('button', { name: 'Sign In' }).click();

  await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
  await expect(page.getByText(data.email)).toBeVisible();
});
