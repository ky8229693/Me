<script setup lang="ts">
import Button from 'primevue/button';
import type { AuthUser } from '../api/types';
import { useI18n } from 'vue-i18n';
import LanguageSelect from '../components/LanguageSelect.vue';

const { t } = useI18n();

defineProps<{
  user: AuthUser;
}>();

const emit = defineEmits<{
  toggleSidebar: [];
  openAssistant: [];
  refresh: [];
  logout: [];
}>();
</script>

<template>
  <header class="layout-topbar">
    <div class="layout-topbar-logo-container">
      <Button
        class="layout-menu-button"
        icon="pi pi-bars"
        text
        rounded
        :aria-label="t('layout.toggleNavigation')"
        @click="emit('toggleSidebar')"
      />
      <div class="layout-topbar-logo" aria-label="WEB Demo">
        <span class="brand-mark small" aria-hidden="true">
          <i class="pi pi-bolt"></i>
        </span>
        <span>WEB Demo</span>
      </div>
    </div>

    <div class="layout-topbar-actions">
      <Button icon="pi pi-sparkles" label="小助手" text :aria-label="'開啟 AI 小助手'" @click="emit('openAssistant')" />
      <Button icon="pi pi-refresh" text rounded :aria-label="t('layout.refreshDashboard')" @click="emit('refresh')" />
      <LanguageSelect />
      <div class="auth-user">
        <i class="pi pi-user" aria-hidden="true"></i>
        <span>{{ user.email }}</span>
      </div>
      <Button icon="pi pi-sign-out" :label="t('layout.logout')" severity="secondary" @click="emit('logout')" />
    </div>
  </header>
</template>
