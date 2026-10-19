<script setup lang="ts">
import { nextTick, ref } from 'vue';
import Button from 'primevue/button';
import Message from 'primevue/message';
import Textarea from 'primevue/textarea';
import { useAssistantState } from './assistantState';

const assistant = useAssistantState();
const input = ref('');
const messageList = ref<HTMLElement | null>(null);

async function submit() {
  const text = input.value.trim();
  if (!text || assistant.loading.value) return;
  input.value = '';
  await assistant.send(text);
  await nextTick();
  messageList.value?.scrollTo({ top: messageList.value.scrollHeight, behavior: 'smooth' });
}
</script>

<template>
  <div class="assistant-panel">
    <div v-if="assistant.conversation.value?.context.projectId" class="assistant-context">
      <i class="pi pi-folder"></i><span>目前專案：{{ assistant.conversation.value.context.projectId }}</span>
      <Button icon="pi pi-times" text rounded size="small" aria-label="移除專案情境" @click="assistant.setContext(null)" />
    </div>
    <div ref="messageList" class="assistant-messages" aria-live="polite">
      <div v-if="!assistant.conversation.value?.messages.length" class="assistant-welcome">
        <i class="pi pi-sparkles"></i>
        <strong>需要我協助什麼？</strong>
        <span>可以詢問操作流程，或請我建立專案與議題。</span>
      </div>
      <div v-for="message in assistant.conversation.value?.messages" :key="message.id"
        class="assistant-bubble" :class="`is-${message.role.toLowerCase()}`">
        {{ message.content }}
      </div>
      <article v-if="assistant.conversation.value?.pendingAction?.status === 'Pending'" class="assistant-preview">
        <strong>請確認{{ assistant.conversation.value.pendingAction.type === 'CreateProject' ? '建立專案' : '建立議題' }}</strong>
        <dl><template v-for="(value, key) in assistant.conversation.value.pendingAction.preview" :key="key">
          <div v-if="value !== null"><dt>{{ key }}</dt><dd>{{ value }}</dd></div>
        </template></dl>
        <div class="assistant-preview-actions">
          <Button label="取消" severity="secondary" size="small" :disabled="assistant.loading.value" @click="assistant.cancel" />
          <Button label="確認執行" icon="pi pi-check" size="small" :loading="assistant.loading.value" @click="assistant.confirm" />
        </div>
      </article>
    </div>
    <Message v-if="assistant.error.value" severity="error" :closable="false">{{ assistant.error.value }}</Message>
    <form class="assistant-composer" @submit.prevent="submit">
      <Textarea v-model="input" rows="3" maxlength="1000" placeholder="輸入訊息，例如：建立一個官網改版專案" fluid
        :disabled="assistant.loading.value" aria-label="小助手訊息" @keydown.ctrl.enter="submit" />
      <div><small>Ctrl + Enter 傳送</small><Button type="submit" icon="pi pi-send" label="傳送" :loading="assistant.loading.value" :disabled="!input.trim()" /></div>
    </form>
  </div>
</template>

<style scoped>
.assistant-panel { height: 100%; display: grid; grid-template-rows: auto 1fr auto auto; gap: .75rem; }
.assistant-context { display: flex; align-items: center; gap: .5rem; padding: .4rem .5rem .4rem .75rem; border-radius: .5rem; background: var(--p-primary-50); color: var(--p-primary-700); font-size: .85rem; }
.assistant-context span { flex: 1; overflow: hidden; text-overflow: ellipsis; }
.assistant-messages { min-height: 0; overflow-y: auto; display: flex; flex-direction: column; gap: .75rem; padding: .25rem; }
.assistant-welcome { margin: auto; display: grid; justify-items: center; gap: .5rem; text-align: center; color: var(--p-text-muted-color); }
.assistant-welcome i { font-size: 2rem; color: var(--p-primary-color); }
.assistant-bubble { max-width: 88%; padding: .75rem .9rem; border-radius: .75rem; white-space: pre-wrap; line-height: 1.5; background: var(--p-content-hover-background); }
.assistant-bubble.is-user { align-self: flex-end; background: var(--p-primary-color); color: var(--p-primary-contrast-color); }
.assistant-preview { padding: 1rem; border: 1px solid var(--p-primary-color); border-radius: .75rem; }
.assistant-preview dl { display: grid; gap: .4rem; }
.assistant-preview dl div { display: grid; grid-template-columns: 7rem 1fr; gap: .5rem; }
.assistant-preview dt { color: var(--p-text-muted-color); }
.assistant-preview dd { margin: 0; overflow-wrap: anywhere; }
.assistant-preview-actions, .assistant-composer > div { display: flex; align-items: center; justify-content: flex-end; gap: .5rem; }
.assistant-composer { display: grid; gap: .5rem; }
.assistant-composer small { margin-right: auto; color: var(--p-text-muted-color); }
</style>
