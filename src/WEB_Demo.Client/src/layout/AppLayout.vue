<script setup lang="ts">
import { computed, ref } from 'vue';
import type { AuthUser } from '../api/types';
import AppSidebar from './AppSidebar.vue';
import AppTopbar from './AppTopbar.vue';
import type { MenuGroup } from './types';
import { hasPermission, permissions } from '../auth/permissions';
import { useI18n } from 'vue-i18n';
import Drawer from 'primevue/drawer';
import AssistantPanel from '../features/assistant/AssistantPanel.vue';
import { useAssistantState } from '../features/assistant/assistantState';
import { useRoute } from 'vue-router';

const props = defineProps<{
  user: AuthUser;
}>();
const { t } = useI18n();
const assistant = useAssistantState();
const route = useRoute();

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

async function openAssistant() {
  await assistant.open();
  const projectId = typeof route.params.projectId === 'string' ? route.params.projectId : null;
  const issueId = typeof route.params.issueId === 'string' ? route.params.issueId : null;
  if (projectId) {
    try { await assistant.setContext(projectId, issueId); } catch { /* API safely rejects unauthorized context. */ }
  }
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
      @open-assistant="openAssistant"
      @refresh="emit('refresh')"
      @logout="emit('logout')"
    />

    <Drawer v-model:visible="assistant.visible.value" position="right" header="AI 小助手"
      class="assistant-drawer" :dismissable="true">
      <AssistantPanel />
    </Drawer>

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

<style>
.assistant-drawer { width: min(30rem, 100vw) !important; }
.assistant-drawer .p-drawer-content { padding-top: .25rem; }
@media (max-width: 640px) { .assistant-drawer { width: 100vw !important; } }
</style>
