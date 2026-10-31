<script setup lang="ts">
import { computed } from 'vue';
import Select from 'primevue/select';
import { usePrimeVue } from 'primevue/config';
import { useI18n } from 'vue-i18n';
import { currentLocale, setLocale, type AppLocale } from '../i18n';

const primevue = usePrimeVue();
const { t } = useI18n();
const options = computed(() => [
  { value: 'zh-TW' as const, label: t('common.traditionalChinese') },
  { value: 'en' as const, label: t('common.english') }
]);
const selected = computed({ get: () => currentLocale.value, set: (value: AppLocale) => setLocale(value, primevue.config) });
</script>

<template>
  <Select v-model="selected" class="language-select" :options="options" option-label="label" option-value="value" :aria-label="t('common.language')" />
</template>
