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

const emit = defineEmits<{
  authenticated: [user: AuthUser];
}>();

const toast = useToast();
const authMode = ref<'login' | 'register'>('login');
const authEmail = ref('');
const authPassword = ref('');
const isAuthLoading = ref(false);
const authErrorMessage = ref('');
const rememberMe = ref(false);
const authModeOptions = [
  { label: 'Login', value: 'login' },
  { label: 'Register', value: 'register' }
];

const authTitle = computed(() => (authMode.value === 'login' ? 'Sign in' : 'Create account'));
const authSubtitle = computed(() => (
  authMode.value === 'login'
    ? 'Welcome back. Enter your credentials to continue.'
    : 'Create an account to start managing your workspace.'
));
const authSubmitLabel = computed(() => (authMode.value === 'login' ? 'Sign In' : 'Create Account'));

function notify(severity: 'success' | 'info' | 'warn' | 'error', detail: string) {
  toast.add({ severity, detail, life: 2600 });
}

async function submitAuth() {
  const email = authEmail.value.trim();
  const password = authPassword.value;
  authErrorMessage.value = '';

  if (!email || !password) {
    authErrorMessage.value = 'Email and password are required.';
    notify('warn', 'Email and password are required.');
    return;
  }

  if (password.length < 6) {
    authErrorMessage.value = 'Password must be at least 6 characters.';
    notify('warn', authErrorMessage.value);
    return;
  }

  isAuthLoading.value = true;

  try {
    const response = authMode.value === 'login'
      ? await login({ email, password })
      : await register({ email, password });

    if (!response?.user) {
      throw new Error('Authentication failed.');
    }

    authPassword.value = '';
    emit('authenticated', response.user);
    notify('success', authMode.value === 'login' ? 'Signed in.' : 'Account created.');
  } catch (error) {
    authErrorMessage.value = error instanceof Error ? error.message : 'Authentication failed.';
    notify('error', authErrorMessage.value);
  } finally {
    isAuthLoading.value = false;
  }
}
</script>

<template>
  <main class="app-shell login-shell">
    <section class="login-panel" aria-label="Authentication">
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

        <SelectButton
          v-model="authMode"
          class="auth-mode"
          :options="authModeOptions"
          option-label="label"
          option-value="value"
          :allow-empty="false"
          aria-label="Authentication mode"
        />

        <form class="auth-form" @submit.prevent="submitAuth">
          <div class="field">
            <label for="auth-email">Email</label>
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
            <label for="auth-password">Password</label>
            <Password
              v-model="authPassword"
              input-id="auth-password"
              :autocomplete="authMode === 'login' ? 'current-password' : 'new-password'"
              :feedback="authMode === 'register'"
              toggle-mask
              placeholder="Password"
              :invalid="Boolean(authErrorMessage)"
              @keyup.enter="submitAuth"
            />
          </div>

          <div class="auth-options">
            <div class="remember-row">
              <Checkbox v-model="rememberMe" input-id="remember-me" binary />
              <label for="remember-me">Remember me</label>
            </div>
            <Button type="button" label="Forgot password?" link />
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
