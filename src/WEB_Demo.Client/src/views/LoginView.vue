<script setup lang="ts">
import { computed, ref } from 'vue';
import Button from 'primevue/button';
import Checkbox from 'primevue/checkbox';
import InputText from 'primevue/inputtext';
import Message from 'primevue/message';
import Password from 'primevue/password';
import SelectButton from 'primevue/selectbutton';
import { useToast } from 'primevue/usetoast';
import { login, register } from '../api/auth';
import type { AuthUser } from '../api/types';
import { useI18n } from 'vue-i18n';
import LanguageSelect from '../components/LanguageSelect.vue';

const emit = defineEmits<{
  authenticated: [user: AuthUser];
}>();

const toast = useToast();
const { t } = useI18n();
const authMode = ref<'login' | 'register'>('login');
const authEmail = ref('');
const authPassword = ref('');
const isAuthLoading = ref(false);
const authErrorMessage = ref('');
const rememberMe = ref(false);
const authModeOptions = computed(() => [
  { label: t('auth.login'), value: 'login' },
  { label: t('auth.register'), value: 'register' }
]);

const authTitle = computed(() => t(authMode.value === 'login' ? 'auth.loginTitle' : 'auth.registerTitle'));
const authSubtitle = computed(() => (
  authMode.value === 'login'
    ? t('auth.loginSubtitle')
    : t('auth.registerSubtitle')
));
const authSubmitLabel = computed(() => t(authMode.value === 'login' ? 'auth.signIn' : 'auth.createAccount'));

function notify(severity: 'success' | 'info' | 'warn' | 'error', detail: string) {
  toast.add({ severity, detail, life: 2600 });
}

async function submitAuth() {
  const email = authEmail.value.trim();
  const password = authPassword.value;
  authErrorMessage.value = '';

  if (!email || !password) {
    authErrorMessage.value = t('auth.required');
    notify('warn', t('auth.required'));
    return;
  }

  if (password.length < 6) {
    authErrorMessage.value = t('auth.passwordLength');
    notify('warn', authErrorMessage.value);
    return;
  }

  isAuthLoading.value = true;

  try {
    const response = authMode.value === 'login'
      ? await login({ email, password })
      : await register({ email, password });

    if (!response?.user) {
      throw new Error(t('auth.failed'));
    }

    authPassword.value = '';
    emit('authenticated', response.user);
    notify('success', t(authMode.value === 'login' ? 'auth.signedIn' : 'auth.accountCreated'));
  } catch (error) {
    authErrorMessage.value = error instanceof Error ? error.message : t('auth.failed');
    notify('error', authErrorMessage.value);
  } finally {
    isAuthLoading.value = false;
  }
}
</script>

<template>
  <main class="app-shell login-shell">
    <section class="login-panel" :aria-label="t('auth.authentication')">
      <div class="login-card">
        <div class="login-brand">
          <span class="brand-mark" aria-hidden="true">
            <i class="pi pi-bolt"></i>
          </span>
          <p class="eyebrow">ASP.NET Core API + Vue 3</p>
          <h1>WEB Demo</h1>
          <h2>{{ authTitle }}</h2>
          <p>{{ authSubtitle }}</p>
        </div>

        <LanguageSelect />
        <SelectButton
          v-model="authMode"
          class="auth-mode"
          :options="authModeOptions"
          option-label="label"
          option-value="value"
          :allow-empty="false"
          :aria-label="t('auth.mode')"
        />

        <form class="auth-form" @submit.prevent="submitAuth">
          <div class="field">
            <label for="auth-email">{{ t('common.email') }}</label>
            <InputText
              id="auth-email"
              v-model="authEmail"
              type="email"
              autocomplete="email"
              placeholder="name@example.com"
              :invalid="Boolean(authErrorMessage)"
            />
          </div>

          <div class="field">
            <label for="auth-password">{{ t('common.password') }}</label>
            <Password
              v-model="authPassword"
              input-id="auth-password"
              :autocomplete="authMode === 'login' ? 'current-password' : 'new-password'"
              :feedback="authMode === 'register'"
              toggle-mask
              :placeholder="t('common.password')"
              :invalid="Boolean(authErrorMessage)"
              @keyup.enter="submitAuth"
            />
          </div>

          <div class="auth-options">
            <div class="remember-row">
              <Checkbox v-model="rememberMe" input-id="remember-me" binary />
              <label for="remember-me">{{ t('auth.remember') }}</label>
            </div>
            <Button type="button" :label="t('auth.forgot')" link />
          </div>

          <Message
            v-if="authErrorMessage"
            class="auth-alert"
            severity="error"
            :closable="false"
          >
            {{ authErrorMessage }}
          </Message>

          <Button
            type="submit"
            class="auth-submit"
            icon="pi pi-sign-in"
            :label="authSubmitLabel"
            :loading="isAuthLoading"
          />
        </form>
      </div>
    </section>
  </main>
</template>
