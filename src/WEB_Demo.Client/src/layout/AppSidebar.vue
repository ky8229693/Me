<script setup lang="ts">
import AppMenu from './AppMenu.vue';
import type { MenuItem } from './types';

defineProps<{
  items: MenuItem[];
}>();

const emit = defineEmits<{
  close: [];
}>();
</script>

<template>
  <aside class="layout-sidebar" aria-label="Main navigation">
    <AppMenu :items="items" @select="emit('close')" />
  </aside>
</template>
