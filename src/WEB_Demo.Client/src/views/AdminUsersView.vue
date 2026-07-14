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
import type { AdminUser } from '../api/types';

const props = defineProps<{ currentUserId: string }>();
const toast = useToast();
const users = ref<AdminUser[]>([]);
const totalCount = ref(0);
const page = ref(1);
const pageSize = ref(10);
const search = ref('');
const loading = ref(false);
const errorMessage = ref('');
const roleOptions = ['User', 'Admin'];
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
    errorMessage.value = error instanceof Error ? error.message : 'Unable to load users.';
  } finally {
    loading.value = false;
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
  if (!window.confirm(`${user.isEnabled ? 'Disable' : 'Enable'} ${user.email}?`)) return;
  try {
    const updated = await setAdminUserEnabled(user.id, !user.isEnabled);
    if (updated) replaceUser(updated);
    toast.add({ severity: 'success', detail: updated?.isEnabled ? 'Account enabled.' : 'Account disabled.', life: 2600 });
  } catch (error) {
    toast.add({ severity: 'error', detail: error instanceof Error ? error.message : 'Unable to update account.', life: 3600 });
  }
}

async function saveRoles(user: AdminUser) {
  try {
    const updated = await setAdminUserRoles(user.id, roleDrafts.value[user.id] ?? []);
    if (updated) replaceUser(updated);
    toast.add({ severity: 'success', detail: 'Roles updated.', life: 2600 });
  } catch (error) {
    roleDrafts.value[user.id] = [...user.roles];
    toast.add({ severity: 'error', detail: error instanceof Error ? error.message : 'Unable to update roles.', life: 3600 });
  }
}

onMounted(loadUsers);
</script>

<template>
  <section class="workspace admin-users-view">
    <header class="page-header">
      <div>
        <p class="eyebrow">Administration</p>
        <h1>User Management</h1>
      </div>
      <div class="admin-search">
        <InputText v-model="search" placeholder="Search by email" aria-label="Search by email" @keyup.enter="searchUsers" />
        <Button label="Search" icon="pi pi-search" :loading="loading" @click="searchUsers" />
      </div>
    </header>

    <Message v-if="errorMessage" severity="error" class="content-alert">{{ errorMessage }}</Message>

    <DataTable :value="users" :loading="loading" data-key="id" striped-rows>
      <template #empty>No users found.</template>
      <Column field="email" header="Email" />
      <Column header="Status">
        <template #body="{ data }">
          <Tag :value="data.isEnabled ? 'Enabled' : 'Disabled'" :severity="data.isEnabled ? 'success' : 'danger'" />
        </template>
      </Column>
      <Column header="Roles">
        <template #body="{ data }">
          <MultiSelect
            v-model="roleDrafts[data.id]"
            :options="roleOptions"
            aria-label="Roles"
            display="chip"
            class="role-select"
            :disabled="data.id === props.currentUserId"
          />
        </template>
      </Column>
      <Column header="Actions">
        <template #body="{ data }">
          <div class="row-actions">
            <Button
              label="Save roles"
              size="small"
              severity="secondary"
              :disabled="data.id === props.currentUserId"
              @click="saveRoles(data)"
            />
            <Button
              :label="data.isEnabled ? 'Disable' : 'Enable'"
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
