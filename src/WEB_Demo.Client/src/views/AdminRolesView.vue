<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import Button from 'primevue/button';
import Column from 'primevue/column';
import DataTable from 'primevue/datatable';
import InputText from 'primevue/inputtext';
import Message from 'primevue/message';
import Tag from 'primevue/tag';
import Checkbox from 'primevue/checkbox';
import Dialog from 'primevue/dialog';
import { useToast } from 'primevue/usetoast';
import { createAdminRole, deleteAdminRole, getAdminRoles, getPermissionCatalog, getRolePermissions, renameAdminRole, updateRolePermissions } from '../api/adminRoles';
import type { AdminRole, AuthUser, PermissionInfo } from '../api/types';
import { hasPermission, permissions as appPermissions } from '../auth/permissions';

const props = defineProps<{ currentUser: AuthUser }>();

const toast = useToast();
const roles = ref<AdminRole[]>([]);
const loading = ref(false);
const errorMessage = ref('');
const newRoleName = ref('');
const roleDrafts = ref<Record<string, string>>({});
const catalog = ref<PermissionInfo[]>([]);
const selectedRole = ref<AdminRole | null>(null);
const permissionDraft = ref<string[]>([]);
const permissionLoading = ref(false);
const canManageRoles = computed(() => hasPermission(props.currentUser, appPermissions.rolesManage));
const canManagePermissions = computed(() => hasPermission(props.currentUser, appPermissions.permissionsManage));
const groupedCatalog = computed(() => catalog.value.reduce<Record<string, PermissionInfo[]>>((groups, item) => {
  (groups[item.category] ??= []).push(item);
  return groups;
}, {}));

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

async function editPermissions(role: AdminRole) {
  permissionLoading.value = true;
  try {
    const [items, mapping] = await Promise.all([getPermissionCatalog(), getRolePermissions(role.id)]);
    if (!items || !mapping) throw new Error('Unable to load role permissions.');
    catalog.value = items;
    permissionDraft.value = [...mapping.permissions];
    selectedRole.value = role;
  } catch (error) {
    toast.add({ severity: 'error', detail: error instanceof Error ? error.message : 'Unable to load permissions.', life: 3600 });
  } finally { permissionLoading.value = false; }
}

async function savePermissions() {
  if (!selectedRole.value) return;
  permissionLoading.value = true;
  try {
    await updateRolePermissions(selectedRole.value.id, permissionDraft.value);
    toast.add({ severity: 'success', detail: 'Permissions updated. Affected sessions were revoked.', life: 3200 });
    selectedRole.value = null;
  } catch (error) {
    toast.add({ severity: 'error', detail: error instanceof Error ? error.message : 'Unable to update permissions.', life: 3600 });
  } finally { permissionLoading.value = false; }
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
        <p>Roles grant API access through explicit permissions. System Admin always has the complete catalog.</p>
      </div>
      <div class="admin-search">
        <InputText v-model="newRoleName" placeholder="New role name" aria-label="New role name" @keyup.enter="createRole" />
        <Button label="Create role" icon="pi pi-plus" :disabled="!canManageRoles || !newRoleName.trim()" :loading="loading" @click="createRole" />
      </div>
    </header>

    <Message v-if="errorMessage" severity="error" class="content-alert">{{ errorMessage }}</Message>

    <DataTable :value="roles" :loading="loading" data-key="id" striped-rows>
      <template #empty>No roles found.</template>
      <Column header="Name">
        <template #body="{ data }">
            <InputText v-model="roleDrafts[data.id]" :disabled="data.isSystem || !canManageRoles" :aria-label="`Role name ${data.name}`" />
        </template>
      </Column>
      <Column header="Type">
        <template #body="{ data }"><Tag :value="data.isSystem ? 'System' : 'Custom'" :severity="data.isSystem ? 'info' : 'secondary'" /></template>
      </Column>
      <Column field="userCount" header="Users" />
      <Column header="Actions">
        <template #body="{ data }">
          <div class="row-actions">
            <Button label="Permissions" size="small" icon="pi pi-key" :disabled="!canManagePermissions" :loading="permissionLoading" @click="editPermissions(data)" />
            <Button label="Rename" size="small" severity="secondary" :disabled="!canManageRoles || data.isSystem || roleDrafts[data.id] === data.name" @click="renameRole(data)" />
            <Button label="Delete" size="small" severity="danger" :disabled="!canManageRoles || data.isSystem || data.userCount > 0" :title="data.userCount > 0 ? 'Remove all user assignments before deleting.' : undefined" @click="removeRole(data)" />
          </div>
        </template>
      </Column>
    </DataTable>

    <Dialog :visible="Boolean(selectedRole)" modal :header="`Permissions · ${selectedRole?.name ?? ''}`" :style="{ width: '42rem' }" @update:visible="selectedRole = null">
      <Message v-if="selectedRole?.name === 'Admin'" severity="info">Admin is the break-glass system role and always has every permission.</Message>
      <div v-for="(items, category) in groupedCatalog" :key="category" class="permission-group">
        <h3>{{ category }}</h3>
        <label v-for="permission in items" :key="permission.key" class="permission-option">
          <Checkbox v-model="permissionDraft" :input-id="permission.key" :value="permission.key" :disabled="selectedRole?.name === 'Admin'" />
          <span><strong>{{ permission.name }}</strong><small>{{ permission.description }}</small></span>
        </label>
      </div>
      <template #footer>
        <Button label="Cancel" severity="secondary" @click="selectedRole = null" />
        <Button label="Save permissions" :disabled="selectedRole?.name === 'Admin'" :loading="permissionLoading" @click="savePermissions" />
      </template>
    </Dialog>
  </section>
</template>

<style scoped>
.permission-group { margin-block: 1rem; }
.permission-option { display: flex; gap: .75rem; align-items: flex-start; padding: .55rem 0; }
.permission-option span { display: grid; gap: .2rem; }
.permission-option small { color: var(--p-text-muted-color); }
</style>
