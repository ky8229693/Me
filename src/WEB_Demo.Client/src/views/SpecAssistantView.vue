<script setup lang="ts">
import { ref } from 'vue';
import Button from 'primevue/button';
import Message from 'primevue/message';
import ProgressSpinner from 'primevue/progressspinner';
import Tag from 'primevue/tag';
import Textarea from 'primevue/textarea';
import { useI18n } from 'vue-i18n';
import { querySpecAssistant } from '../api/specAssistant';
import type { SpecAssistantQueryResponse } from '../api/types';

const { t } = useI18n();
const question = ref('');
const loading = ref(false);
const error = ref('');
const result = ref<SpecAssistantQueryResponse | null>(null);

async function ask() {
  const value = question.value.trim();
  if (!value) {
    error.value = t('specAssistant.required');
    return;
  }

  loading.value = true;
  error.value = '';
  try {
    result.value = await querySpecAssistant(value);
  } catch {
    error.value = t('specAssistant.failed');
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <section class="workspace spec-assistant-workspace">
    <header class="page-header">
      <div>
        <p class="eyebrow">{{ t('specAssistant.eyebrow') }}</p>
        <h1>{{ t('specAssistant.title') }}</h1>
        <p>{{ t('specAssistant.subtitle') }}</p>
      </div>
    </header>

    <Message severity="info" :closable="false">{{ t('specAssistant.phaseNote') }}</Message>

    <form class="assistant-question-card" @submit.prevent="ask">
      <label for="spec-question">{{ t('specAssistant.question') }}</label>
      <Textarea id="spec-question" v-model="question" rows="5" :maxlength="1000"
        :placeholder="t('specAssistant.placeholder')" :disabled="loading" fluid />
      <div class="assistant-actions">
        <small>{{ question.length }} / 1000</small>
        <Button type="submit" icon="pi pi-send" :label="t('specAssistant.ask')" :loading="loading" :disabled="loading" />
      </div>
    </form>

    <div v-if="loading" class="assistant-loading">
      <ProgressSpinner :aria-label="t('specAssistant.asking')" />
      <span>{{ t('specAssistant.asking') }}</span>
    </div>

    <Message v-else-if="error" severity="error">
      <div class="assistant-error"><span>{{ error }}</span><Button :label="t('common.retry')" size="small" @click="ask" /></div>
    </Message>

    <article v-else-if="result" class="assistant-answer-card">
      <h2>{{ t('specAssistant.answer') }}</h2>
      <p class="assistant-answer">{{ result.answer }}</p>

      <template v-if="result.relatedSpecs.length">
        <h3>{{ t('specAssistant.relatedSpecs') }}</h3>
        <div class="assistant-tags"><Tag v-for="spec in result.relatedSpecs" :key="spec" :value="spec" severity="secondary" /></div>
      </template>

      <h3>{{ t('specAssistant.sources') }}</h3>
      <ul v-if="result.sources.length" class="assistant-sources">
        <li v-for="source in result.sources" :key="source"><code>{{ source }}</code></li>
      </ul>
      <p v-else class="muted">{{ t('specAssistant.noSources') }}</p>
    </article>
  </section>
</template>

<style scoped>
.spec-assistant-workspace { display: grid; gap: 1rem; max-width: 960px; }
.assistant-question-card, .assistant-answer-card { padding: 1.25rem; border: 1px solid var(--p-content-border-color); border-radius: 12px; background: var(--p-content-background); }
.assistant-question-card { display: grid; gap: .75rem; }
.assistant-question-card label, .assistant-answer-card h2 { font-weight: 700; }
.assistant-actions, .assistant-error { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
.assistant-loading { display: flex; align-items: center; justify-content: center; gap: 1rem; padding: 2rem; }
.assistant-loading :deep(.p-progressspinner) { width: 2rem; height: 2rem; }
.assistant-answer { white-space: pre-wrap; line-height: 1.7; }
.assistant-tags { display: flex; flex-wrap: wrap; gap: .5rem; }
.assistant-sources { padding-left: 1.25rem; overflow-wrap: anywhere; }
</style>
