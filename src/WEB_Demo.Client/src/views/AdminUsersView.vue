<script setup lang="ts">
import { onMounted, ref } from 'vue';
import Button from 'primevue/button';
import Column from 'primevue/column';
import DataTable from 'primevue/datatable';
import InputText from 'primevue/inputtext';
import Message from 'primevue/message';
import MultiSelect from 'primevue/multiselect';
import Paginator, { type PageState } from 'primevue/paginator';
import Tag from 'primevue/tag';
import { useToast } from 'primevue/usetoast';
import { getAdminUsers, setAdminUserEnabled, setAdminUserRoles } from '../api/adminUsers';
import { getAdminRoles } from '../api/adminRoles';
import type { AdminUser } from '../api/types';
import { useI18n } from 'vue-i18n';
import { useDomainLabels } from '../i18n/domain';

const props = defineProps<{ currentUserId: string }>();
const toast = useToast();
const { t } = useI18n();
const { roleLabel } = useDomainLabels();
const users = ref<AdminUser[]>([]);
const totalCount = ref(0);
const page = ref(1);
const pageSize = ref(10);
const search = ref('');
const loading = ref(false);
const errorMessage = ref('');
const roleOptions = ref<string[]>([]);
const rolesLoading = ref(false);
const rolesError = ref('');
const roleDrafts = ref<Record<string, string[]>>({});

async function loadUsers() {
  loading.value = true;
  errorMessage.value = '';
  try {
    const result = await getAdminUsers(page.value, pageSize.value, search.value);
    users.value = result?.items ?? [];
    totalCount.value = result?.totalCount ?? 0;
    roleDrafts.value = Object.fromEntries(users.value.map(user => [user.id, [...user.roles]]));
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : t('admin.loadUsersFailed');
  } finally {
    loading.value = false;
  }
}

async function loadRoles() {
  rolesLoading.value = true;
  rolesError.value = '';
  try {
    roleOptions.value = (await getAdminRoles())?.map(role => role.name) ?? [];
  } catch (error) {
    roleOptions.value = [];
    rolesError.value = error instanceof Error ? error.message : 'Unable to load roles.';
  } finally {
    rolesLoading.value = false;
  }
}

function searchUsers() {
  page.value = 1;
  void loadUsers();
}

function changePage(event: PageState) {
  page.value = event.page + 1;
  pageSize.value = event.rows;
  void loadUsers();
}

function replaceUser(updated: AdminUser) {
  const index = users.value.findIndex(user => user.id === updated.id);
  if (index >= 0) users.value[index] = updated;
  roleDrafts.value[updated.id] = [...updated.roles];
}

async function toggleEnabled(user: AdminUser) {
  if (!window.confirm(`${t(user.isEnabled ? 'admin.disable' : 'admin.enable')} ${user.email}?`)) return;
  try {
    const updated = await setAdminUserEnabled(user.id, !user.isEnabled);
    if (updated) replaceUser(updated);
    toast.add({ severity: 'success', detail: t(updated?.isEnabled ? 'admin.accountEnabled' : 'admin.accountDisabled'), life: 2600 });
  } catch (error) {
    toast.add({ severity: 'error', detail: error instanceof Error ? error.message : t('common.error'), life: 3600 });
  }
}

async function saveRoles(user: AdminUser) {
  if (rolesError.value || rolesLoading.value) return;
  try {
    const updated = await setAdminUserRoles(user.id, roleDrafts.value[user.id] ?? []);
    if (updated) replaceUser(updated);
    toast.add({ severity: 'success', detail: t('admin.rolesUpdated'), life: 2600 });
  } catch (error) {
    roleDrafts.value[user.id] = [...user.roles];
    toast.add({ severity: 'error', detail: error instanceof Error ? error.message : t('common.error'), life: 3600 });
  }
}

onMounted(() => {
  void Promise.all([loadUsers(), loadRoles()]);
});
</script>

<template>
  <section class="workspace admin-users-view">
    <header class="page-header">
      <div>
        <p class="eyebrow">{{t('admin.administration')}}</p>
        <h1>{{t('admin.usersTitle')}}</h1>
      </div>
      <div class="admin-search">
        <InputText v-model="search" :placeholder="t('admin.searchEmail')" :aria-label="t('admin.searchEmail')" @keyup.enter="searchUsers" />
        <Button :label="t('common.search')" icon="pi pi-search" :loading="loading" @click="searchUsers" />
      </div>
    </header>

    <Message v-if="errorMessage" severity="error" class="content-alert">{{ errorMessage }}</Message>
    <Message v-if="rolesError" severity="error" class="content-alert">
      {{t('admin.rolesLoadFailed')}} {{ rolesError }}
    </Message>

    <DataTable :value="users" :loading="loading" data-key="id" striped-rows>
      <template #empty>{{t('admin.noUsers')}}</template>
      <Column field="email" :header="t('common.email')" />
      <Column :header="t('common.status')">
        <template #body="{ data }">
          <Tag :value="t(data.isEnabled ? 'admin.enabled' : 'common.disabled')" :severity="data.isEnabled ? 'success' : 'danger'" />
        </template>
      </Column>
      <Column :header="t('admin.assignedRoles')">
        <template #body="{ data }">
          <MultiSelect
            v-model="roleDrafts[data.id]"
            :options="roleOptions.map(value=>({value,label:roleLabel(value)}))"
            option-label="label"
            option-value="value"
            :aria-label="t('admin.assignedRoles')"
            display="chip"
            class="role-select"
            :disabled="data.id === props.currentUserId || rolesLoading || !!rolesError"
          />
        </template>
      </Column>
      <Column :header="t('common.actions')">
        <template #body="{ data }">
          <div class="row-actions">
            <Button
              :label="t('admin.saveRoles')"
              size="small"
              severity="secondary"
              :disabled="data.id === props.currentUserId || rolesLoading || !!rolesError"
              @click="saveRoles(data)"
            />
            <Button
              :label="t(data.isEnabled ? 'admin.disable' : 'admin.enable')"
              size="small"
              :severity="data.isEnabled ? 'danger' : 'success'"
              :disabled="data.id === props.currentUserId"
              @click="toggleEnabled(data)"
            />
          </div>
        </template>
      </Column>
    </DataTable>

    <Paginator
      :first="(page - 1) * pageSize"
      :rows="pageSize"
      :total-records="totalCount"
      :rows-per-page-options="[10, 25, 50]"
      @page="changePage"
    />
  </section>
</template>
