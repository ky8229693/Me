<script setup lang="ts">
import { RouterLink, useRoute } from 'vue-router';
import type { MenuGroup } from './types';

defineProps<{
  groups: MenuGroup[];
}>();

const emit = defineEmits<{
  select: [];
}>();
const route = useRoute();
</script>

<template>
  <nav>
    <section v-for="group in groups.filter(item => item.items.length)" :key="group.label">
      <p class="menu-section">{{ group.label }}</p>
      <RouterLink v-for="item in group.items" :key="item.routeName" class="menu-item"
        :class="{ active: route.name === item.routeName || (item.routeName === 'projects' && String(route.name).startsWith('project-')) }"
        :to="{ name: item.routeName }" @click="emit('select')">
        <i :class="item.icon" aria-hidden="true"></i><span>{{ item.label }}</span>
      </RouterLink>
    </section>
  </nav>
</template>
