import { expect, test, type Locator } from '@playwright/test';
import { registerThroughUi } from './support/auth';
import { createE2ETestData } from './support/testData';

async function readSummaryCount(locator: Locator): Promise<number> {
  const value = await locator.textContent();
  const count = value?.match(/\d+/)?.[0];

  if (count === undefined) {
    throw new Error(`Unable to read todo summary count from: ${value ?? '<empty>'}`);
  }

  return Number(count);
}

test('使用者可以新增、完成並刪除自己建立的 Todo', async ({ page }) => {
  const data = createE2ETestData();

  await registerThroughUi(page, data);

  const pendingSummary = page.getByText(/\d+ pending/, { exact: true });
  const completedSummary = page.getByText(/\d+ done/, { exact: true });
  const initialPending = await readSummaryCount(pendingSummary);
  const initialCompleted = await readSummaryCount(completedSummary);

  await page.getByPlaceholder('Add a todo item').fill(data.todoTitle);
  await page.getByRole('button', { name: 'Add', exact: true }).click();

  const todoRow = page.getByRole('row').filter({ hasText: data.todoTitle });
  await expect(todoRow).toBeVisible();
  await expect(pendingSummary).toHaveText(`${initialPending + 1} pending`);

  await todoRow.getByRole('checkbox').click();

  await expect(todoRow.getByText('Done', { exact: true })).toBeVisible();
  await expect(pendingSummary).toHaveText(`${initialPending} pending`);
  await expect(completedSummary).toHaveText(`${initialCompleted + 1} done`);

  await todoRow.getByRole('button', { name: 'Delete' }).click();

  await expect(todoRow).toHaveCount(0);
});
