import { createMemoryHistory } from 'vue-router';
import { createAppRouter } from '../router';

export async function createTestRouter(initialPath = '/dashboard') {
  const router = createAppRouter(createMemoryHistory());
  await router.push(initialPath);
  await router.isReady();
  return router;
}
