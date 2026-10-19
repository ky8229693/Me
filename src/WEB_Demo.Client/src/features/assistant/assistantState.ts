import { ref } from 'vue';
import { cancelAssistantAction, confirmAssistantAction, createAssistantConversation, getAssistantConversation, sendAssistantMessage, updateAssistantContext } from '../../api/assistant';
import type { AssistantConversation } from '../../api/types';

const STORAGE_KEY = 'web-demo.assistant-conversation';
const visible = ref(false);
const conversation = ref<AssistantConversation | null>(null);
const loading = ref(false);
const error = ref('');

export function useAssistantState() {
  async function ensureConversation() {
    if (conversation.value) return conversation.value;
    const stored = sessionStorage.getItem(STORAGE_KEY);
    if (stored) {
      try { conversation.value = await getAssistantConversation(stored); }
      catch { sessionStorage.removeItem(STORAGE_KEY); }
    }
    if (!conversation.value) conversation.value = await createAssistantConversation();
    if (conversation.value) sessionStorage.setItem(STORAGE_KEY, conversation.value.id);
    return conversation.value;
  }

  async function open() {
    visible.value = true;
    error.value = '';
    try { await ensureConversation(); }
    catch { error.value = '無法載入小助手對話。'; }
  }

  async function send(text: string) {
    const active = await ensureConversation();
    if (!active) return;
    loading.value = true; error.value = '';
    const optimistic = { id: crypto.randomUUID(), role: 'User' as const, content: text, createdAt: new Date().toISOString() };
    active.messages.push(optimistic);
    try {
      const turn = await sendAssistantMessage(active.id, text);
      if (turn) {
        active.messages.push(turn.message);
        active.pendingAction = turn.pendingAction;
      }
    } catch { error.value = '小助手目前無法回覆，請稍後再試。'; }
    finally { loading.value = false; }
  }

  async function confirm() {
    const active = conversation.value;
    if (!active?.pendingAction) return;
    loading.value = true; error.value = '';
    try {
      const result = await confirmAssistantAction(active.id, active.pendingAction.id);
      if (result) {
        active.pendingAction = result.action;
        active.messages.push({ id: crypto.randomUUID(), role: 'Assistant', content: result.message, createdAt: new Date().toISOString() });
      }
    } catch { error.value = '確認動作失敗，資料尚未建立。'; }
    finally { loading.value = false; }
  }

  async function cancel() {
    const active = conversation.value;
    if (!active?.pendingAction) return;
    loading.value = true;
    try { await cancelAssistantAction(active.id, active.pendingAction.id); active.pendingAction = null; }
    catch { error.value = '取消動作失敗。'; }
    finally { loading.value = false; }
  }

  async function setContext(projectId: string | null, issueId: string | null = null) {
    const active = await ensureConversation();
    if (!active) return;
    if (active.context.projectId === projectId && active.context.issueId === issueId) return;
    const updated = await updateAssistantContext(active.id, projectId, issueId);
    if (updated) conversation.value = updated;
  }

  return { visible, conversation, loading, error, open, send, confirm, cancel, setContext };
}
