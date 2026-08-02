<script setup lang="ts">
import AppMenu from './AppMenu.vue';
import type { MenuGroup } from './types';
import { useI18n } from 'vue-i18n';
const { t } = useI18n();

defineProps<{
  groups: MenuGroup[];
}>();

const emit = defineEmits<{
  close: [];
}>();

function select() {
  emit('close');
}
</script>

<template>
  <aside class="layout-sidebar" :aria-label="t('layout.mainNavigation')">
    <AppMenu :groups="groups" @select="select" />
  </aside>
</template>
