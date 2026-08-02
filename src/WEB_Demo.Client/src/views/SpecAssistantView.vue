<script setup lang="ts">
import { onMounted, ref } from 'vue';
import Button from 'primevue/button';
import Message from 'primevue/message';
import ProgressSpinner from 'primevue/progressspinner';
import Tag from 'primevue/tag';
import Textarea from 'primevue/textarea';
import { useI18n } from 'vue-i18n';
import { getSpecIndexStatus, querySpecAssistant, rebuildSpecIndex } from '../api/specAssistant';
import type { SpecAssistantQueryResponse, SpecIndexStatus } from '../api/types';

const { t } = useI18n();
const question = ref('');
const loading = ref(false);
const error = ref('');
const result = ref<SpecAssistantQueryResponse | null>(null);
const indexStatus = ref<SpecIndexStatus | null>(null);
const indexLoading = ref(false);
const indexError = ref('');

async function loadIndexStatus() {
  indexError.value = '';
  try { indexStatus.value = await getSpecIndexStatus(); }
  catch { indexError.value = t('specAssistant.indexLoadFailed'); }
}

async function rebuildIndex() {
  indexLoading.value = true;
  indexError.value = '';
  try { indexStatus.value = await rebuildSpecIndex(); }
  catch { indexError.value = t('specAssistant.indexRebuildFailed'); }
  finally { indexLoading.value = false; }
}

onMounted(loadIndexStatus);

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

    <section class="assistant-index-card">
      <div>
        <p class="eyebrow">{{ t('specAssistant.retrieval') }}</p>
        <h2>{{ indexStatus?.retrievalMode ?? '—' }}</h2>
      </div>
      <dl v-if="indexStatus" class="assistant-index-details">
        <div><dt>{{ t('specAssistant.indexStatus') }}</dt><dd>{{ indexStatus.status }}</dd></div>
        <div><dt>{{ t('specAssistant.embeddingModel') }}</dt><dd>{{ indexStatus.model ?? '—' }}</dd></div>
        <div><dt>{{ t('specAssistant.chunkCount') }}</dt><dd>{{ indexStatus.chunkCount }}</dd></div>
        <div><dt>{{ t('specAssistant.builtAt') }}</dt><dd>{{ indexStatus.builtAt ? new Date(indexStatus.builtAt).toLocaleString() : '—' }}</dd></div>
      </dl>
      <Button v-if="indexStatus && indexStatus.retrievalMode !== 'Lexical'" icon="pi pi-refresh"
        :label="t('specAssistant.rebuildIndex')" severity="secondary" :loading="indexLoading"
        :disabled="indexLoading" @click="rebuildIndex" />
      <Message v-if="indexError" severity="error" :closable="false">{{ indexError }}</Message>
    </section>

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
.assistant-index-card { display: grid; grid-template-columns: auto 1fr auto; align-items: center; gap: 1rem; padding: 1rem 1.25rem; border: 1px solid var(--p-content-border-color); border-radius: 12px; background: var(--p-content-background); }
.assistant-index-card h2 { margin: 0; }
.assistant-index-details { display: flex; flex-wrap: wrap; gap: .75rem 1.5rem; margin: 0; }
.assistant-index-details div { display: grid; gap: .2rem; }
.assistant-index-details dt { color: var(--p-text-muted-color); font-size: .8rem; }
.assistant-index-details dd { margin: 0; font-weight: 600; }
.assistant-question-card { display: grid; gap: .75rem; }
.assistant-question-card label, .assistant-answer-card h2 { font-weight: 700; }
.assistant-actions, .assistant-error { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
.assistant-loading { display: flex; align-items: center; justify-content: center; gap: 1rem; padding: 2rem; }
.assistant-loading :deep(.p-progressspinner) { width: 2rem; height: 2rem; }
.assistant-answer { white-space: pre-wrap; line-height: 1.7; }
.assistant-tags { display: flex; flex-wrap: wrap; gap: .5rem; }
.assistant-sources { padding-left: 1.25rem; overflow-wrap: anywhere; }
@media (max-width: 720px) { .assistant-index-card { grid-template-columns: 1fr; } }
</style>
