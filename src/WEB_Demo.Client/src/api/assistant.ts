import { requestApi } from './http';
import type { AssistantConfirmation, AssistantConversation, AssistantConversationSummary, AssistantPendingAction, AssistantTurn } from './types';

export async function createAssistantConversation(projectId?: string | null, issueId?: string | null) {
  return requestApi<AssistantConversation>('/api/assistant/conversations', {
    method: 'POST', body: { projectId: projectId ?? null, issueId: issueId ?? null }
  });
}
export async function listAssistantConversations() {
  return (await requestApi<AssistantConversationSummary[]>('/api/assistant/conversations')) ?? [];
}
export async function getAssistantConversation(id: string) {
  return requestApi<AssistantConversation>(`/api/assistant/conversations/${id}`);
}
export async function sendAssistantMessage(id: string, message: string) {
  return requestApi<AssistantTurn>(`/api/assistant/conversations/${id}/messages`, { method: 'POST', body: { message } });
}
export async function confirmAssistantAction(conversationId: string, actionId: string) {
  return requestApi<AssistantConfirmation>(`/api/assistant/conversations/${conversationId}/actions/${actionId}/confirm`, { method: 'POST' });
}
export async function cancelAssistantAction(conversationId: string, actionId: string) {
  return requestApi<AssistantPendingAction>(`/api/assistant/conversations/${conversationId}/actions/${actionId}/cancel`, { method: 'POST' });
}
export async function updateAssistantContext(id: string, projectId: string | null, issueId: string | null) {
  return requestApi<AssistantConversation>(`/api/assistant/conversations/${id}/context`, {
    method: 'PUT', body: { projectId, issueId }
  });
}
