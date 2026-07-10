<script setup lang="ts">
import type { MenuItem } from './types';

defineProps<{
  items: MenuItem[];
}>();

const emit = defineEmits<{
  select: [];
}>();
</script>

<template>
  <nav>
    <p class="menu-section">Home</p>
    <button
      v-for="item in items"
      :key="item.label"
      type="button"
      class="menu-item"
      :class="{ active: item.active }"
      @click="emit('select')"
    >
      <i :class="item.icon" aria-hidden="true"></i>
      <span>{{ item.label }}</span>
    </button>
  </nav>
</template>
