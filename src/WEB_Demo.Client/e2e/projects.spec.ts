import { expect, test } from '@playwright/test';
import { loginThroughUi } from './support/auth';
import { createE2ETestData, e2eAdmin } from './support/testData';

test('Project Owner 可以建立專案、加入成員並封存與還原', async ({ page, request }) => {
  const target = createE2ETestData();
  const register = await request.post('/api/auth/register', {
    data: { email: target.email, password: target.password }
  });
  expect(register.status()).toBe(201);
  const registered = await register.json();
  expect(registered.data.user.id).toBeTruthy();
  const projectName = `PMS_${Date.now()}`;

  await loginThroughUi(page, e2eAdmin.email, e2eAdmin.password);
  await page.getByRole('button', { name: 'Projects' }).click();
  await page.getByRole('button', { name: 'Create project' }).click();
  await page.getByRole('dialog', { name: 'Create project' }).getByLabel('Name').fill(projectName);
  await page.getByRole('dialog', { name: 'Create project' }).getByRole('button', { name: 'Create', exact: true }).click();

  const projectRow = page.getByRole('row').filter({ hasText: projectName });
  await expect(projectRow).toBeVisible();
  await projectRow.getByRole('button', { name: 'Open' }).click();
  const dialog = page.getByRole('dialog', { name: projectName });
  await dialog.getByLabel('Existing user email').fill('e2e-');
  await page.getByRole('option', { name: target.email, exact: true }).click();
  await dialog.getByRole('button', { name: 'Add member' }).click();
  await expect(dialog.getByRole('row').filter({ hasText: target.email })).toBeVisible();

  await dialog.getByRole('button', { name: 'Archive' }).click();
  await expect(dialog.getByText('This project is archived and read-only.')).toBeVisible();
  await dialog.getByRole('button', { name: 'Restore' }).click();
  await expect(dialog.getByRole('button', { name: 'Archive' })).toBeVisible();
  await dialog.getByRole('button', { name: 'Close' }).click();

  await page.getByRole('button', { name: 'Logout' }).click();
  await loginThroughUi(page, target.email, target.password);
  await page.getByRole('button', { name: 'Projects' }).click();
  await expect(page.getByRole('row').filter({ hasText: projectName })).toBeVisible();
});

test('沒有 projects.create 的一般 User 仍可開啟 Projects 但不能建立專案', async ({ page, request }) => {
  const target = createE2ETestData();
  await request.post('/api/auth/register', { data: { email: target.email, password: target.password } });
  await loginThroughUi(page, target.email, target.password);
  await page.getByRole('button', { name: 'Projects' }).click();
  await expect(page.getByRole('heading', { name: 'Projects' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Create project' })).toHaveCount(0);
});
