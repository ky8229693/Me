import { expect, test } from '@playwright/test';
import { createE2ETestData } from './support/testData';

test('預設繁體中文並保存英文切換', async ({ page }) => {
  const data = createE2ETestData();
  await page.goto('/');

  await expect(page.getByRole('heading', { name: '登入' })).toBeVisible();
  await expect(page.locator('html')).toHaveAttribute('lang', 'zh-TW');

  await page.getByRole('combobox', { name: '介面語言' }).click();
  await page.getByRole('option', { name: 'English', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
  await page.reload();
  await expect(page.locator('html')).toHaveAttribute('lang', 'en');
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();

  await page.getByRole('button', { name: 'Register' }).click();
  await page.getByLabel('Email').fill(data.email);
  await page.getByLabel('Password', { exact: true }).fill(data.password);
  await page.getByRole('button', { name: 'Create Account' }).click();
  await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();

  await page.getByRole('combobox', { name: 'Interface language' }).click();
  await page.getByRole('option', { name: '繁體中文', exact: true }).click();
  await expect(page.getByRole('heading', { name: '工作台' })).toBeVisible();
  await expect(page.locator('html')).toHaveAttribute('lang', 'zh-TW');
});
