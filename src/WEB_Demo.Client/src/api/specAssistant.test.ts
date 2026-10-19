import { beforeEach, describe, expect, it, vi } from 'vitest';
import { confirmProjectDraft, createProjectDraft, getSpecIndexStatus, querySpecAssistant, rebuildSpecIndex } from './specAssistant';
import { requestApi } from './http';

vi.mock('./http', () => ({ requestApi: vi.fn() }));

describe('spec assistant API', () => {
  beforeEach(() => vi.clearAllMocks());

  it('posts the question to the centralized endpoint', async () => {
    await querySpecAssistant('Which specification?');
    expect(requestApi).toHaveBeenCalledWith('/api/spec-assistant/query', {
      method: 'POST', body: { question: 'Which specification?' }
    });
  });

  it('uses authenticated index status and rebuild endpoints', async () => {
    await getSpecIndexStatus();
    await rebuildSpecIndex();
    expect(requestApi).toHaveBeenNthCalledWith(1, '/api/spec-assistant/index');
    expect(requestApi).toHaveBeenNthCalledWith(2, '/api/spec-assistant/index/rebuild', { method: 'POST' });
  });

  it('creates and confirms a project draft through controlled endpoints', async () => {
    await createProjectDraft('Create Portal');
    await confirmProjectDraft('draft-1');
    expect(requestApi).toHaveBeenNthCalledWith(1, '/api/spec-assistant/project-drafts', { method: 'POST', body: { prompt: 'Create Portal' } });
    expect(requestApi).toHaveBeenNthCalledWith(2, '/api/spec-assistant/project-drafts/draft-1/confirm', { method: 'POST' });
  });
});
