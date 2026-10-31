import { expect, test } from '@playwright/test';
import { loginThroughUi, registerThroughUi } from './support/auth';
import { createE2ETestData, e2eAdmin } from './support/testData';

test('Admin 可以建立、指派、改名並刪除自訂 role', async ({ page }) => {
  const target = createE2ETestData();
  const suffix = Date.now().toString();
  const roleName = `Support_${suffix}`;
  const renamedRole = `Operations_${suffix}`;
  await registerThroughUi(page, target);
  await page.getByRole('button', { name: 'Logout' }).click();
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
  await loginThroughUi(page, e2eAdmin.email, e2eAdmin.password);

  await page.getByRole('link', { name: 'Role Management' }).click();
  await page.getByLabel('New role name').fill(roleName);
  await page.getByRole('button', { name: 'Create role' }).click();
  const roleRow = page.getByRole('row').filter({ has: page.getByLabel(`Role name ${roleName}`) });
  await expect(roleRow).toBeVisible();
  await roleRow.getByRole('button', { name: 'Permissions' }).click();
  await page.getByText('Read roles', { exact: true }).click();
  await page.getByRole('button', { name: 'Save permissions' }).click();

  await page.getByRole('link', { name: 'User Management' }).click();
  await page.getByLabel('Search by email').fill(target.email);
  await page.getByRole('button', { name: 'Search', exact: true }).click();
  const userRow = page.getByRole('row').filter({ hasText: target.email });
  await userRow.getByRole('combobox', { name: 'Roles' }).press('ArrowDown');
  await page.getByRole('option', { name: roleName, exact: true }).click();
  await page.keyboard.press('Escape');
  await userRow.getByRole('button', { name: 'Save roles' }).click();

  await page.getByRole('button', { name: 'Logout' }).click();
  await loginThroughUi(page, target.email, target.password);
  await expect(page.getByRole('link', { name: 'Role Management' })).toBeVisible();
  await page.getByRole('link', { name: 'Role Management' }).click();
  await expect(page.getByRole('button', { name: 'Create role' })).toBeDisabled();
  await page.getByRole('button', { name: 'Logout' }).click();
  await loginThroughUi(page, e2eAdmin.email, e2eAdmin.password);

  await page.getByRole('link', { name: 'Role Management' }).click();
  await expect(page.getByRole('row').filter({ has: page.getByLabel(`Role name ${roleName}`) })
    .getByRole('button', { name: 'Delete' })).toBeDisabled();

  await page.getByRole('link', { name: 'User Management' }).click();
  await page.getByLabel('Search by email').fill(target.email);
  await page.getByRole('button', { name: 'Search', exact: true }).click();
  const updatedUserRow = page.getByRole('row').filter({ hasText: target.email });
  await updatedUserRow.getByRole('combobox', { name: 'Roles' }).press('ArrowDown');
  await page.getByRole('option', { name: roleName, exact: true }).click();
  await page.keyboard.press('Escape');
  await updatedUserRow.getByRole('button', { name: 'Save roles' }).click();

  await page.getByRole('link', { name: 'Role Management' }).click();
  const finalRow = page.getByRole('row').filter({ has: page.getByLabel(`Role name ${roleName}`) });
  await finalRow.getByLabel(`Role name ${roleName}`).fill(renamedRole);
  await finalRow.getByRole('button', { name: 'Rename' }).click();
  const renamedRow = page.getByRole('row').filter({ has: page.getByLabel(`Role name ${renamedRole}`) });
  page.once('dialog', dialog => dialog.accept());
  await renamedRow.getByRole('button', { name: 'Delete' }).click();
  await expect(page.getByLabel(`Role name ${renamedRole}`)).toHaveCount(0);
});

test('一般 User 看不到 Role Management 且 Role API 回傳 403', async ({ page }) => {
  await registerThroughUi(page, createE2ETestData());
  await expect(page.getByRole('link', { name: 'Role Management' })).toHaveCount(0);
  const token = await page.evaluate(async () => {
    const response = await fetch('/api/auth/refresh', { method: 'POST', credentials: 'include' });
    return (await response.json()).data.accessToken as string;
  });
  const response = await page.request.get('/api/admin/roles', {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(response.status()).toBe(403);
});
