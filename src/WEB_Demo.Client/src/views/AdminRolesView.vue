<script setup lang="ts">
import { onMounted, ref } from 'vue';
import Button from 'primevue/button';
import Column from 'primevue/column';
import DataTable from 'primevue/datatable';
import InputText from 'primevue/inputtext';
import Message from 'primevue/message';
import Tag from 'primevue/tag';
import { useToast } from 'primevue/usetoast';
import { createAdminRole, deleteAdminRole, getAdminRoles, renameAdminRole } from '../api/adminRoles';
import type { AdminRole } from '../api/types';

const toast = useToast();
const roles = ref<AdminRole[]>([]);
const loading = ref(false);
const errorMessage = ref('');
const newRoleName = ref('');
const roleDrafts = ref<Record<string, string>>({});

async function loadRoles() {
  loading.value = true;
  errorMessage.value = '';
  try {
    roles.value = (await getAdminRoles()) ?? [];
    roleDrafts.value = Object.fromEntries(roles.value.map(role => [role.id, role.name]));
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Unable to load roles.';
  } finally {
    loading.value = false;
  }
}

function replaceRole(updated: AdminRole) {
  const index = roles.value.findIndex(role => role.id === updated.id);
  if (index >= 0) roles.value[index] = updated;
  roleDrafts.value[updated.id] = updated.name;
}

async function createRole() {
  try {
    const created = await createAdminRole(newRoleName.value);
    if (created) {
      roles.value.push(created);
      roles.value.sort((left, right) => left.name.localeCompare(right.name));
      roleDrafts.value[created.id] = created.name;
    }
    newRoleName.value = '';
    toast.add({ severity: 'success', detail: 'Role created.', life: 2600 });
  } catch (error) {
    toast.add({ severity: 'error', detail: error instanceof Error ? error.message : 'Unable to create role.', life: 3600 });
  }
}

async function renameRole(role: AdminRole) {
  try {
    const updated = await renameAdminRole(role.id, roleDrafts.value[role.id] ?? '');
    if (updated) replaceRole(updated);
    toast.add({ severity: 'success', detail: 'Role renamed.', life: 2600 });
  } catch (error) {
    roleDrafts.value[role.id] = role.name;
    toast.add({ severity: 'error', detail: error instanceof Error ? error.message : 'Unable to rename role.', life: 3600 });
  }
}

async function removeRole(role: AdminRole) {
  if (!window.confirm(`Delete role ${role.name}?`)) return;
  try {
    await deleteAdminRole(role.id);
    roles.value = roles.value.filter(item => item.id !== role.id);
    toast.add({ severity: 'success', detail: 'Role deleted.', life: 2600 });
  } catch (error) {
    toast.add({ severity: 'error', detail: error instanceof Error ? error.message : 'Unable to delete role.', life: 3600 });
    await loadRoles();
  }
}

onMounted(loadRoles);
</script>

<template>
  <section class="workspace admin-roles-view">
    <header class="page-header">
      <div>
        <p class="eyebrow">Administration</p>
        <h1>Role Management</h1>
        <p>Custom roles are labels for assignment and do not automatically grant permissions.</p>
      </div>
      <div class="admin-search">
        <InputText v-model="newRoleName" placeholder="New role name" aria-label="New role name" @keyup.enter="createRole" />
        <Button label="Create role" icon="pi pi-plus" :disabled="!newRoleName.trim()" :loading="loading" @click="createRole" />
      </div>
    </header>

    <Message v-if="errorMessage" severity="error" class="content-alert">{{ errorMessage }}</Message>

    <DataTable :value="roles" :loading="loading" data-key="id" striped-rows>
      <template #empty>No roles found.</template>
      <Column header="Name">
        <template #body="{ data }">
          <InputText v-model="roleDrafts[data.id]" :disabled="data.isSystem" :aria-label="`Role name ${data.name}`" />
        </template>
      </Column>
      <Column header="Type">
        <template #body="{ data }"><Tag :value="data.isSystem ? 'System' : 'Custom'" :severity="data.isSystem ? 'info' : 'secondary'" /></template>
      </Column>
      <Column field="userCount" header="Users" />
      <Column header="Actions">
        <template #body="{ data }">
          <div class="row-actions">
            <Button label="Rename" size="small" severity="secondary" :disabled="data.isSystem || roleDrafts[data.id] === data.name" @click="renameRole(data)" />
            <Button label="Delete" size="small" severity="danger" :disabled="data.isSystem || data.userCount > 0" :title="data.userCount > 0 ? 'Remove all user assignments before deleting.' : undefined" @click="removeRole(data)" />
          </div>
        </template>
      </Column>
    </DataTable>
  </section>
</template>
