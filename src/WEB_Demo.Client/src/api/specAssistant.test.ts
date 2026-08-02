import { beforeEach, describe, expect, it, vi } from 'vitest';
import { querySpecAssistant } from './specAssistant';
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
});
