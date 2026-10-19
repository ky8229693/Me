import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises } from '@vue/test-utils';
import * as api from '../../api/assistant';
import { useAssistantState } from './assistantState';
import type { AssistantConversation, AssistantTurn } from '../../api/types';

vi.mock('../../api/assistant', () => ({
  createAssistantConversation: vi.fn(), getAssistantConversation: vi.fn(),
  sendAssistantMessage: vi.fn(), confirmAssistantAction: vi.fn(),
  cancelAssistantAction: vi.fn(), updateAssistantContext: vi.fn()
}));

const emptyConversation = (): AssistantConversation => ({
  id: 'conversation-1', title: null, context: { projectId: null, issueId: null },
  messages: [], pendingAction: null,
  createdAt: '2026-10-19T00:00:00Z', updatedAt: '2026-10-19T00:00:00Z'
});

describe('assistantState', () => {
  const assistant = useAssistantState();

  beforeEach(() => {
    vi.clearAllMocks();
    sessionStorage.clear();
    assistant.conversation.value = null;
    assistant.loading.value = false;
    assistant.error.value = '';
    vi.mocked(api.createAssistantConversation).mockResolvedValue(emptyConversation());
  });

  it('keeps a visible activity state until the tool request completes', async () => {
    let complete!: (turn: AssistantTurn) => void;
    vi.mocked(api.sendAssistantMessage).mockImplementation(() =>
      new Promise(resolve => { complete = resolve; }));

    const pending = assistant.send('建立測試議題');
    await flushPromises();
    expect(assistant.loading.value).toBe(true);

    complete({
      message: { id: 'assistant-1', role: 'Assistant', content: '沒有執行任何操作。', createdAt: '2026-10-19T00:01:00Z' },
      pendingAction: null, sources: [], state: 'tool_required'
    });
    await pending;

    expect(assistant.loading.value).toBe(false);
    const messages = assistant.conversation.value?.messages ?? [];
    expect(messages[messages.length - 1]?.content).toContain('沒有執行任何操作');
  });

  it('states that no operation ran when the request fails', async () => {
    vi.mocked(api.sendAssistantMessage).mockRejectedValue(new Error('provider failed'));

    await assistant.send('建立測試議題');

    expect(assistant.error.value).toContain('沒有執行任何操作');
    expect(assistant.loading.value).toBe(false);
  });
});
