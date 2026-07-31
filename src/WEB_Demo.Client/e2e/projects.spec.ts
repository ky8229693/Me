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
  await page.getByRole('link', { name: 'Projects' }).click();
  await page.getByRole('button', { name: 'Create project' }).click();
  await page.getByRole('dialog', { name: 'Create project' }).getByLabel('Name').fill(projectName);
  await page.getByRole('dialog', { name: 'Create project' }).getByRole('button', { name: 'Create', exact: true }).click();

  await expect(page.getByRole('heading', { name: projectName })).toBeVisible();
  const projectNavigation = page.getByRole('navigation', { name: 'Project navigation' });
  await projectNavigation.getByRole('link', { name: 'Members' }).click();
  await page.getByLabel('Existing user email').fill('e2e-');
  await page.getByRole('option', { name: target.email, exact: true }).click();
  await page.getByRole('button', { name: 'Add member' }).click();
  await expect(page.getByRole('row').filter({ hasText: target.email })).toBeVisible();

  await projectNavigation.getByRole('link', { name: 'Settings' }).click();
  await page.getByRole('button', { name: 'Archive project' }).click();
  await expect(page.getByText('This project is archived and all project work is read-only.')).toBeVisible();
  await page.getByRole('button', { name: 'Restore project' }).click();
  await expect(page.getByRole('button', { name: 'Archive project' })).toBeVisible();

  await page.getByRole('button', { name: 'Logout' }).click();
  await loginThroughUi(page, target.email, target.password);
  await page.getByRole('link', { name: 'Projects' }).click();
  await expect(page.getByRole('row').filter({ hasText: projectName })).toBeVisible();
});

test('沒有 projects.create 的一般 User 仍可開啟 Projects 但不能建立專案', async ({ page, request }) => {
  const target = createE2ETestData();
  await request.post('/api/auth/register', { data: { email: target.email, password: target.password } });
  await loginThroughUi(page, target.email, target.password);
  await page.getByRole('link', { name: 'Projects' }).click();
  await expect(page.getByRole('heading', { name: 'Projects' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Create project' })).toHaveCount(0);
});
