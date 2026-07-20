<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue';
import Toast from 'primevue/toast';
import { useToast } from 'primevue/usetoast';
import { logout, refreshSession } from './api/auth';
import type { AuthUser } from './api/types';
import AppLayout from './layout/AppLayout.vue';
import LoginView from './views/LoginView.vue';
import ForbiddenView from './views/ForbiddenView.vue';
import { onSessionExpired } from './api/authSession';
import { hasPermission } from './auth/permissions';
import { RouterView, useRoute, useRouter } from 'vue-router';

const toast = useToast();
const authUser = ref<AuthUser | null>(null);
const authLoading = ref(true);
const dashboardRefreshKey = ref(0);
const route = useRoute();
const router = useRouter();
let stopSessionExpiredListener: (() => void) | null = null;
const routeAllowed = computed(() => !route.meta.permission || Boolean(authUser.value && hasPermission(authUser.value, route.meta.permission)));

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
  dashboardRefreshKey.value += 1;
  if (route.name === 'not-found') void router.push({ name: 'dashboard' });
}

function refreshDashboard() {
  dashboardRefreshKey.value += 1;
}

function handleSessionExpired() {
  authUser.value = null;
  toast.add({ severity: 'warn', detail: 'Your session has expired. Please sign in again.', life: 4000 });
}

async function signOut() {
  await logout();
  authUser.value = null;
  await router.push({ name: 'dashboard' });
  toast.add({ severity: 'success', detail: 'Signed out.', life: 2600 });
}

onMounted(() => {
  stopSessionExpiredListener = onSessionExpired(handleSessionExpired);
  void loadCurrentUser();
});

onUnmounted(() => stopSessionExpiredListener?.());
</script>

<template>
  <Toast position="top-center" />

  <div v-if="authLoading" class="auth-loading">Restoring session...</div>

  <LoginView v-else-if="!authUser" @authenticated="handleAuthenticated" />

  <AppLayout
    v-else
    :user="authUser"
    @refresh="refreshDashboard"
    @logout="signOut"
  >
    <ForbiddenView v-if="!routeAllowed" />
    <RouterView v-else v-slot="{ Component }">
      <component :is="Component" :current-user="authUser" :current-user-id="authUser.id" :refresh-key="dashboardRefreshKey" />
    </RouterView>
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
