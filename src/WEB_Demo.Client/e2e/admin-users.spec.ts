import { expect, test } from '@playwright/test';
import { loginThroughUi, registerThroughUi } from './support/auth';
import { createE2ETestData, e2eAdmin } from './support/testData';

test('Admin 可以搜尋、停用、啟用並提升測試帳號', async ({ page }) => {
  const target = createE2ETestData();
  await registerThroughUi(page, target);
  await page.getByRole('button', { name: 'Logout' }).click();
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
  await loginThroughUi(page, e2eAdmin.email, e2eAdmin.password);

  await page.getByRole('link', { name: 'User Management' }).click();
  await expect(page.getByRole('heading', { name: 'User Management' })).toBeVisible();
  await page.getByLabel('Search by email').fill(target.email);
  await page.getByRole('button', { name: 'Search', exact: true }).click();

  const row = page.getByRole('row').filter({ hasText: target.email });
  await expect(row).toBeVisible();

  page.once('dialog', dialog => dialog.accept());
  await row.getByRole('button', { name: 'Disable' }).click();
  await expect(row.getByText('Disabled', { exact: true })).toBeVisible();

  page.once('dialog', dialog => dialog.accept());
  await row.getByRole('button', { name: 'Enable' }).click();
  await expect(row.getByText('Enabled', { exact: true })).toBeVisible();

  await row.getByRole('combobox', { name: 'Roles' }).press('ArrowDown');
  await page.getByRole('option', { name: 'Admin', exact: true }).click();
  await page.keyboard.press('Escape');
  await row.getByRole('button', { name: 'Save roles' }).click();
  await expect(row.getByText('Admin', { exact: true })).toBeVisible();
});

test('一般使用者看不到管理入口且管理 API 回傳 403', async ({ page }) => {
  const data = createE2ETestData();
  await registerThroughUi(page, data);

  await expect(page.getByRole('link', { name: 'User Management' })).toHaveCount(0);
  const token = await page.evaluate(async () => {
    const response = await fetch('/api/auth/refresh', { method: 'POST', credentials: 'include' });
    return (await response.json()).data.accessToken as string;
  });
  const response = await page.request.get('/api/admin/users', {
    headers: { Authorization: `Bearer ${token}` }
  });

  expect(response.status()).toBe(403);
  await expect(response.json()).resolves.toMatchObject({ success: false, errorCode: 'FORBIDDEN' });
});
