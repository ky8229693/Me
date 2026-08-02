<script setup lang="ts">
import { computed, ref } from 'vue';
import type { AuthUser } from '../api/types';
import AppSidebar from './AppSidebar.vue';
import AppTopbar from './AppTopbar.vue';
import type { MenuGroup } from './types';
import { hasPermission, permissions } from '../auth/permissions';
import { useI18n } from 'vue-i18n';

const props = defineProps<{
  user: AuthUser;
}>();
const { t } = useI18n();

const emit = defineEmits<{
  logout: [];
  refresh: [];
}>();

const isSidebarOpen = ref(false);
const menuGroups = computed<MenuGroup[]>(() => {
  const administration: MenuGroup['items'] = [];
  if (hasPermission(props.user, permissions.usersRead)) {
    administration.push({ routeName: 'users', label: t('layout.users'), icon: 'pi pi-users' });
  }
  if (hasPermission(props.user, permissions.rolesRead)) {
    administration.push({ routeName: 'roles', label: t('layout.roles'), icon: 'pi pi-shield' });
  }
  return [
    { label: t('layout.workbench'), items: [
      { routeName: 'dashboard', label: t('layout.dashboard'), icon: 'pi pi-home' },
      { routeName: 'spec-assistant', label: t('layout.specAssistant'), icon: 'pi pi-sparkles' }
    ] },
    { label: t('layout.projectManagement'), items: [{ routeName: 'projects', label: t('layout.projects'), icon: 'pi pi-briefcase' }] },
    { label: t('layout.systemAdministration'), items: administration }
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
      :aria-label="t('layout.closeNavigation')"
      @click="closeSidebar"
    ></button>

    <main class="layout-main-container">
      <slot />
    </main>
  </div>
</template>
