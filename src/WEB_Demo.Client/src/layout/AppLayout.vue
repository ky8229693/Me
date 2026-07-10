<script setup lang="ts">
import { ref } from 'vue';
import type { AuthUser } from '../api/types';
import AppSidebar from './AppSidebar.vue';
import AppTopbar from './AppTopbar.vue';
import type { MenuItem } from './types';

defineProps<{
  user: AuthUser;
}>();

const emit = defineEmits<{
  logout: [];
  refresh: [];
}>();

const isSidebarOpen = ref(false);
const menuItems: MenuItem[] = [
  { label: 'Dashboard', icon: 'pi pi-home', active: true },
  { label: 'Todos', icon: 'pi pi-check-square', active: true },
  { label: 'API Health', icon: 'pi pi-heart', active: false },
  { label: 'Settings', icon: 'pi pi-cog', active: false }
];

function toggleSidebar() {
  isSidebarOpen.value = !isSidebarOpen.value;
}

function closeSidebar() {
  isSidebarOpen.value = false;
}
</script>

<template>
  <div class="layout-wrapper" :class="{ 'layout-mobile-active': isSidebarOpen }">
    <AppTopbar
      :user="user"
      @toggle-sidebar="toggleSidebar"
      @refresh="emit('refresh')"
      @logout="emit('logout')"
    />

    <AppSidebar :items="menuItems" @close="closeSidebar" />

    <button
      v-if="isSidebarOpen"
      type="button"
      class="layout-mask"
      aria-label="Close navigation"
      @click="closeSidebar"
    ></button>

    <main class="layout-main-container">
      <slot />
    </main>
  </div>
</template>
