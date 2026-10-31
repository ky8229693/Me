import { expect, test } from '@playwright/test';
import { loginThroughUi } from './support/auth';
import { e2eAdmin } from './support/testData';

test('Project Overview 顯示議題摘要並可導向議題詳情', async ({ page }) => {
  const suffix = Date.now();
  const projectName = `Overview_${suffix}`;
  const issueTitle = `Overview issue ${suffix}`;

  await loginThroughUi(page, e2eAdmin.email, e2eAdmin.password);
  await page.getByRole('link', { name: 'Projects' }).click();
  await page.getByRole('button', { name: 'Create project' }).click();
  const projectDialog = page.getByRole('dialog', { name: 'Create project' });
  await projectDialog.getByLabel('Name').fill(projectName);
  await projectDialog.getByRole('button', { name: 'Create', exact: true }).click();

  const projectNavigation = page.getByRole('navigation', { name: 'Project navigation' });
  await projectNavigation.getByRole('link', { name: 'Issues' }).click();
  await page.getByRole('button', { name: 'Create Issue' }).click();
  const issueDialog = page.getByRole('dialog', { name: 'Create Issue' });
  await issueDialog.getByText('Title').locator('input').fill(issueTitle);
  await issueDialog.getByRole('button', { name: 'Create', exact: true }).click();
  await expect(page.getByRole('heading', { name: issueTitle })).toBeVisible();

  await projectNavigation.getByRole('link', { name: 'Overview' }).click();
  await expect(page.getByText('Total Issues').locator('..').getByText('1', { exact: true })).toBeVisible();
  await expect(page.getByText('Issue status distribution')).toBeVisible();
  await expect(page.getByRole('link').filter({ hasText: issueTitle })).toBeVisible();

  await page.getByRole('link').filter({ hasText: issueTitle }).click();
  await expect(page.getByRole('heading', { name: issueTitle })).toBeVisible();
});
