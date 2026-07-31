<script setup lang="ts">
import { computed, ref } from 'vue';
import type { AuthUser } from '../api/types';
import AppSidebar from './AppSidebar.vue';
import AppTopbar from './AppTopbar.vue';
import type { MenuGroup } from './types';
import { hasPermission, permissions } from '../auth/permissions';

const props = defineProps<{
  user: AuthUser;
}>();

const emit = defineEmits<{
  logout: [];
  refresh: [];
}>();

const isSidebarOpen = ref(false);
const menuGroups = computed<MenuGroup[]>(() => {
  const administration: MenuGroup['items'] = [];
  if (hasPermission(props.user, permissions.usersRead)) {
    administration.push({ routeName: 'users', label: 'User Management', icon: 'pi pi-users' });
  }
  if (hasPermission(props.user, permissions.rolesRead)) {
    administration.push({ routeName: 'roles', label: 'Role Management', icon: 'pi pi-shield' });
  }
  return [
    { label: 'Workbench', items: [{ routeName: 'dashboard', label: 'Dashboard', icon: 'pi pi-home' }] },
    { label: 'Project Management', items: [{ routeName: 'projects', label: 'Projects', icon: 'pi pi-briefcase' }] },
    { label: 'System Administration', items: administration }
  ];
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

    <AppSidebar :groups="menuGroups" @close="closeSidebar" />

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
