<script setup lang="ts">
import { computed, ref } from 'vue';
import type { AuthUser } from '../api/types';
import AppSidebar from './AppSidebar.vue';
import AppTopbar from './AppTopbar.vue';
import type { MenuItem } from './types';
import { hasPermission, permissions } from '../auth/permissions';

const props = defineProps<{
  user: AuthUser;
  activeView: MenuItem['id'];
}>();

const emit = defineEmits<{
  logout: [];
  refresh: [];
  navigate: [id: MenuItem['id']];
}>();

const isSidebarOpen = ref(false);
const menuItems = computed<MenuItem[]>(() => {
  const items: MenuItem[] = [
    { id: 'dashboard', label: 'Dashboard', icon: 'pi pi-home', active: props.activeView === 'dashboard' }
  ];
  if (hasPermission(props.user, permissions.usersRead)) {
    items.push({ id: 'users', label: 'User Management', icon: 'pi pi-users', active: props.activeView === 'users' });
  }
  if (hasPermission(props.user, permissions.rolesRead)) {
    items.push({ id: 'roles', label: 'Role Management', icon: 'pi pi-shield', active: props.activeView === 'roles' });
  }
  return items;
});

function toggleSidebar() {
  isSidebarOpen.value = !isSidebarOpen.value;
}

function closeSidebar() {
  isSidebarOpen.value = false;
}
</script>

<template>
  <div
    class="layout-wrapper"
    :class="{
      'layout-sidebar-collapsed': isSidebarOpen,
      'layout-mobile-active': isSidebarOpen
    }"
  >
    <AppTopbar
      :user="user"
      @toggle-sidebar="toggleSidebar"
      @refresh="emit('refresh')"
      @logout="emit('logout')"
    />

    <AppSidebar :items="menuItems" @close="closeSidebar" @navigate="emit('navigate', $event)" />

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
