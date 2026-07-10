<script setup lang="ts">
import { onMounted, ref } from 'vue';
import Toast from 'primevue/toast';
import { useToast } from 'primevue/usetoast';
import { getCurrentUser, logout } from './api/auth';
import type { AuthUser } from './api/types';
import AppLayout from './layout/AppLayout.vue';
import DashboardView from './views/DashboardView.vue';
import LoginView from './views/LoginView.vue';

const toast = useToast();
const authUser = ref<AuthUser | null>(null);
const dashboardRefreshKey = ref(0);

async function loadCurrentUser() {
  try {
    authUser.value = await getCurrentUser();
  } catch {
    authUser.value = null;
    logout();
  }
}

function handleAuthenticated(user: AuthUser) {
  authUser.value = user;
  dashboardRefreshKey.value += 1;
}

function refreshDashboard() {
  dashboardRefreshKey.value += 1;
}

function signOut() {
  logout();
  authUser.value = null;
  toast.add({ severity: 'success', detail: 'Signed out.', life: 2600 });
}

onMounted(loadCurrentUser);
</script>

<template>
  <Toast position="top-center" />

  <LoginView v-if="!authUser" @authenticated="handleAuthenticated" />

  <AppLayout v-else :user="authUser" @refresh="refreshDashboard" @logout="signOut">
    <DashboardView :refresh-key="dashboardRefreshKey" />
  </AppLayout>
</template>
