import { expect, test } from '@playwright/test';
import { loginThroughUi } from './support/auth';
import { e2eAdmin } from './support/testData';

test('Dashboard 可導向帶標籤的 Kanban Issue 並推進 workflow', async ({ page }) => {
  const suffix=Date.now();const projectName=`Kanban_${suffix}`;const issueTitle=`Board issue ${suffix}`;const labelName=`Release-${suffix}`;
  await loginThroughUi(page,e2eAdmin.email,e2eAdmin.password);
  await page.getByRole('link',{name:'Projects'}).click();await page.getByRole('button',{name:'Create project'}).click();
  await page.getByRole('dialog',{name:'Create project'}).getByLabel('Name').fill(projectName);await page.getByRole('dialog',{name:'Create project'}).getByRole('button',{name:'Create',exact:true}).click();
  await page.getByRole('navigation',{name:'Project navigation'}).getByRole('link',{name:'Issues'}).click();
  await page.getByRole('button',{name:'Labels'}).click();await page.getByPlaceholder('New label name').fill(labelName);await page.getByRole('dialog',{name:'Manage labels'}).getByRole('button',{name:'Add'}).click();await expect(page.locator(`input[value="${labelName}"]`)).toBeVisible();await page.keyboard.press('Escape');
  await page.getByRole('button',{name:'Create Issue'}).click();const dialog=page.getByRole('dialog',{name:'Create Issue'});await dialog.getByText('Title').locator('input').fill(issueTitle);await dialog.locator('.p-select').last().click();await page.getByRole('option',{name:e2eAdmin.email}).click();await dialog.locator('.p-multiselect').click();await page.getByRole('option',{name:labelName}).click();await page.keyboard.press('Escape');await dialog.getByRole('button',{name:'Create',exact:true}).click();
  await page.getByRole('button',{name:'Back to Issues'}).click();await page.getByRole('button',{name:'Board',exact:true}).click();await expect(page.getByRole('button',{name:issueTitle})).toBeVisible();
  await page.getByRole('link',{name:'Dashboard'}).click();await expect(page.getByRole('button').filter({hasText:issueTitle})).toBeVisible();await page.getByRole('button').filter({hasText:issueTitle}).click();await page.getByRole('button',{name:'Move to Approved'}).click();await expect(page.getByText('Approved',{exact:true})).toBeVisible();
});
