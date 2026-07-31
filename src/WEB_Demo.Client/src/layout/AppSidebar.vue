<script setup lang="ts">
import AppMenu from './AppMenu.vue';
import type { MenuGroup } from './types';

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
  <aside class="layout-sidebar" aria-label="Main navigation">
    <AppMenu :groups="groups" @select="select" />
  </aside>
</template>
