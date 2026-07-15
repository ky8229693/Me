<script setup lang="ts">
import { onMounted, ref } from 'vue';
import Toast from 'primevue/toast';
import { useToast } from 'primevue/usetoast';
import { logout, refreshSession } from './api/auth';
import type { AuthUser } from './api/types';
import AppLayout from './layout/AppLayout.vue';
import DashboardView from './views/DashboardView.vue';
import AdminUsersView from './views/AdminUsersView.vue';
import AdminRolesView from './views/AdminRolesView.vue';
import LoginView from './views/LoginView.vue';
import type { MenuItem } from './layout/types';

const toast = useToast();
const authUser = ref<AuthUser | null>(null);
const dashboardRefreshKey = ref(0);
const activeView = ref<MenuItem['id']>('dashboard');
const authLoading = ref(true);

async function loadCurrentUser() {
  try {
    authUser.value = (await refreshSession())?.user ?? null;
  } catch {
    authUser.value = null;
  } finally {
    authLoading.value = false;
  }
}

function handleAuthenticated(user: AuthUser) {
  authUser.value = user;
  activeView.value = 'dashboard';
  dashboardRefreshKey.value += 1;
}

function refreshDashboard() {
  dashboardRefreshKey.value += 1;
}

async function signOut() {
  await logout();
  authUser.value = null;
  activeView.value = 'dashboard';
  toast.add({ severity: 'success', detail: 'Signed out.', life: 2600 });
}

onMounted(loadCurrentUser);
</script>

<template>
  <Toast position="top-center" />

  <div v-if="authLoading" class="auth-loading">Restoring session...</div>

  <LoginView v-else-if="!authUser" @authenticated="handleAuthenticated" />

  <AppLayout
    v-else
    :user="authUser"
    :active-view="activeView"
    @navigate="activeView = $event"
    @refresh="refreshDashboard"
    @logout="signOut"
  >
    <AdminUsersView v-if="activeView === 'users' && authUser.roles.includes('Admin')" :current-user-id="authUser.id" />
    <AdminRolesView v-else-if="activeView === 'roles' && authUser.roles.includes('Admin')" />
    <DashboardView v-else :refresh-key="dashboardRefreshKey" />
  </AppLayout>
</template>

<style scoped>
.auth-loading {
  min-height: 100vh;
  display: grid;
  place-items: center;
  color: var(--p-text-muted-color);
}
</style>
