<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import Button from 'primevue/button';
import AutoComplete from 'primevue/autocomplete';
import Column from 'primevue/column';
import DataTable from 'primevue/datatable';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Message from 'primevue/message';
import Select from 'primevue/select';
import Tag from 'primevue/tag';
import Textarea from 'primevue/textarea';
import { useToast } from 'primevue/usetoast';
import type { AuthUser, ProjectInfo, ProjectMemberInfo } from '../api/types';
import {
  addProjectMember, archiveProject, createProject, getProjectMembers, getProjects,
  projectRoles, removeProjectMember, restoreProject, searchProjectMemberCandidates,
  updateProject, updateProjectMemberRole
} from '../api/projects';
import { hasPermission, permissions } from '../auth/permissions';
import IssueWorkspace from '../features/issues/IssueWorkspace.vue';

const props = defineProps<{ currentUser: AuthUser }>();
const toast = useToast();
const projects = ref<ProjectInfo[]>([]);
const selectedProject = ref<ProjectInfo | null>(null);
const members = ref<ProjectMemberInfo[]>([]);
const loading = ref(false);
const memberLoading = ref(false);
const errorMessage = ref('');
const createVisible = ref(false);
const createName = ref('');
const createDescription = ref('');
const editName = ref('');
const editDescription = ref('');
const newMemberEmail = ref('');
const memberEmailSuggestions = ref<string[]>([]);
let candidateRequestId = 0;
const newMemberRole = ref<(typeof projectRoles)[number]>('Member');
const roleDrafts = ref<Record<string, string>>({});

const canCreate = computed(() => hasPermission(props.currentUser, permissions.projectsCreate));
const canEdit = computed(() => selectedProject.value?.status === 'Active' && ['Owner', 'Manager'].includes(selectedProject.value.currentUserRole));
const isOwner = computed(() => selectedProject.value?.currentUserRole === 'Owner');
const canManageMembers = computed(() => selectedProject.value?.status === 'Active' && ['Owner', 'Manager'].includes(selectedProject.value.currentUserRole));
const assignableRoles = computed(() => isOwner.value ? [...projectRoles] : ['Member', 'Viewer']);

async function loadProjects() {
  loading.value = true;
  errorMessage.value = '';
  try { projects.value = (await getProjects()) ?? []; }
  catch (error) { errorMessage.value = error instanceof Error ? error.message : 'Unable to load projects.'; }
  finally { loading.value = false; }
}

async function openProject(project: ProjectInfo) {
  selectedProject.value = project;
  editName.value = project.name;
  editDescription.value = project.description ?? '';
  memberLoading.value = true;
  try {
    members.value = (await getProjectMembers(project.id)) ?? [];
    roleDrafts.value = Object.fromEntries(members.value.map(member => [member.userId, member.role]));
  } catch (error) {
    toast.add({ severity: 'error', detail: error instanceof Error ? error.message : 'Unable to load project members.', life: 3600 });
  } finally { memberLoading.value = false; }
}

function replaceProject(updated: ProjectInfo) {
  const index = projects.value.findIndex(project => project.id === updated.id);
  if (index >= 0) projects.value[index] = updated;
  selectedProject.value = updated;
  editName.value = updated.name;
  editDescription.value = updated.description ?? '';
}

async function submitProject() {
  try {
    const created = await createProject(createName.value, createDescription.value.trim() || null);
    if (created) projects.value.push(created);
    createName.value = '';
    createDescription.value = '';
    createVisible.value = false;
    projects.value.sort((left, right) => left.name.localeCompare(right.name));
    toast.add({ severity: 'success', detail: 'Project created.', life: 2600 });
  } catch (error) { toast.add({ severity: 'error', detail: error instanceof Error ? error.message : 'Unable to create project.', life: 3600 }); }
}

async function saveProject() {
  if (!selectedProject.value) return;
  try {
    const updated = await updateProject(selectedProject.value.id, editName.value, editDescription.value.trim() || null);
    if (updated) replaceProject(updated);
    toast.add({ severity: 'success', detail: 'Project updated.', life: 2600 });
  } catch (error) { toast.add({ severity: 'error', detail: error instanceof Error ? error.message : 'Unable to update project.', life: 3600 }); }
}

async function toggleArchive() {
  if (!selectedProject.value) return;
  try {
    const updated = selectedProject.value.status === 'Active'
      ? await archiveProject(selectedProject.value.id)
      : await restoreProject(selectedProject.value.id);
    if (updated) replaceProject(updated);
    toast.add({ severity: 'success', detail: updated?.status === 'Archived' ? 'Project archived.' : 'Project restored.', life: 2600 });
  } catch (error) { toast.add({ severity: 'error', detail: error instanceof Error ? error.message : 'Unable to change project status.', life: 3600 }); }
}

async function addMember() {
  if (!selectedProject.value) return;
  try {
    const created = await addProjectMember(selectedProject.value.id, newMemberEmail.value.trim(), newMemberRole.value);
    if (created) {
      members.value.push(created);
      roleDrafts.value[created.userId] = created.role;
    }
    newMemberEmail.value = '';
    toast.add({ severity: 'success', detail: 'Project member added.', life: 2600 });
  } catch (error) { toast.add({ severity: 'error', detail: error instanceof Error ? error.message : 'Unable to add member.', life: 3600 }); }
}

async function searchMemberEmails(event: { query: string }) {
  const projectId = selectedProject.value?.id;
  const query = event.query.trim();
  const requestId = ++candidateRequestId;
  if (!projectId || query.length < 2) {
    memberEmailSuggestions.value = [];
    return;
  }
  try {
    const candidates = await searchProjectMemberCandidates(projectId, query);
    if (requestId === candidateRequestId) memberEmailSuggestions.value = candidates?.map(item => item.email) ?? [];
  } catch (error) {
    if (requestId === candidateRequestId) memberEmailSuggestions.value = [];
    toast.add({ severity: 'error', detail: error instanceof Error ? error.message : 'Unable to search users.', life: 3600 });
  }
}

async function saveMemberRole(member: ProjectMemberInfo) {
  if (!selectedProject.value) return;
  try {
    const updated = await updateProjectMemberRole(selectedProject.value.id, member.userId, roleDrafts.value[member.userId]);
    if (updated) {
      const index = members.value.findIndex(item => item.userId === updated.userId);
      if (index >= 0) members.value[index] = updated;
    }
    toast.add({ severity: 'success', detail: 'Project role updated.', life: 2600 });
  } catch (error) {
    roleDrafts.value[member.userId] = member.role;
    toast.add({ severity: 'error', detail: error instanceof Error ? error.message : 'Unable to update member.', life: 3600 });
  }
}

async function removeMember(member: ProjectMemberInfo) {
  if (!selectedProject.value || !window.confirm(`Remove ${member.email}?`)) return;
  try {
    await removeProjectMember(selectedProject.value.id, member.userId);
    members.value = members.value.filter(item => item.userId !== member.userId);
    toast.add({ severity: 'success', detail: 'Project member removed.', life: 2600 });
  } catch (error) { toast.add({ severity: 'error', detail: error instanceof Error ? error.message : 'Unable to remove member.', life: 3600 }); }
}

function canManageMember(member: ProjectMemberInfo) {
  return isOwner.value || member.role === 'Member' || member.role === 'Viewer';
}

onMounted(loadProjects);
</script>

<template>
  <section class="workspace projects-view">
    <header class="page-header">
      <div><p class="eyebrow">PMS</p><h1>Projects</h1><p>Project membership is the access boundary for all PMS data.</p></div>
      <Button v-if="canCreate" label="Create project" icon="pi pi-plus" @click="createVisible = true" />
    </header>
    <Message v-if="errorMessage" severity="error" class="content-alert">{{ errorMessage }}</Message>
    <DataTable :value="projects" :loading="loading" data-key="id" striped-rows selection-mode="single" @row-click="openProject($event.data)">
      <template #empty>No accessible projects.</template>
      <Column field="name" header="Name" />
      <Column field="currentUserRole" header="Your role" />
      <Column header="Status"><template #body="{ data }"><Tag :value="data.status" :severity="data.status === 'Active' ? 'success' : 'secondary'" /></template></Column>
      <Column header="Actions"><template #body="{ data }"><Button label="Open" size="small" severity="secondary" @click.stop="openProject(data)" /></template></Column>
    </DataTable>

    <Dialog :visible="Boolean(selectedProject)" modal :header="selectedProject?.name ?? 'Project'" :style="{ width: 'min(64rem, 94vw)' }" @update:visible="selectedProject = null">
      <Message v-if="selectedProject?.status === 'Archived'" severity="warn">This project is archived and read-only.</Message>
      <div class="project-form">
        <label>Name<InputText v-model="editName" :disabled="!canEdit" /></label>
        <label>Description<Textarea v-model="editDescription" rows="3" :disabled="!canEdit" /></label>
        <div class="row-actions">
          <Button label="Save project" :disabled="!canEdit || !editName.trim()" @click="saveProject" />
          <Button v-if="isOwner" :label="selectedProject?.status === 'Active' ? 'Archive' : 'Restore'" severity="secondary" @click="toggleArchive" />
        </div>
      </div>

      <h2>Members</h2>
      <div v-if="canManageMembers" class="member-create">
        <AutoComplete
          v-model="newMemberEmail"
          :suggestions="memberEmailSuggestions"
          :min-length="2"
          :delay="250"
          type="email"
          placeholder="Type at least 2 email characters"
          aria-label="Existing user email"
          @complete="searchMemberEmails"
        />
        <Select v-model="newMemberRole" :options="assignableRoles" aria-label="Project role" />
        <Button label="Add member" :disabled="!newMemberEmail.trim()" @click="addMember" />
      </div>
      <DataTable :value="members" :loading="memberLoading" data-key="userId" striped-rows>
        <Column field="email" header="User" />
        <Column header="Project role">
          <template #body="{ data }">
            <Select
              v-if="canManageMembers && canManageMember(data)"
              v-model="roleDrafts[data.userId]"
              :options="assignableRoles"
              :aria-label="`Project role ${data.email}`"
            />
            <Tag
              v-else
              :value="data.role"
              severity="secondary"
              :data-testid="`project-role-${data.userId}`"
            />
          </template>
        </Column>
        <Column header="Actions"><template #body="{ data }"><div class="row-actions"><Button label="Save role" size="small" :disabled="!canManageMembers || !canManageMember(data) || roleDrafts[data.userId] === data.role" @click="saveMemberRole(data)" /><Button label="Remove" size="small" severity="danger" :disabled="!canManageMembers || !canManageMember(data)" @click="removeMember(data)" /></div></template></Column>
      </DataTable>
      <IssueWorkspace v-if="selectedProject" :project="selectedProject" :members="members" :current-user="currentUser" />
    </Dialog>

    <Dialog v-model:visible="createVisible" modal header="Create project" :style="{ width: '32rem' }">
      <div class="project-form"><label>Name<InputText v-model="createName" autofocus /></label><label>Description<Textarea v-model="createDescription" rows="4" /></label></div>
      <template #footer><Button label="Cancel" severity="secondary" @click="createVisible = false" /><Button label="Create" :disabled="!createName.trim()" @click="submitProject" /></template>
    </Dialog>
  </section>
</template>

<style scoped>
.project-form, .project-form label { display: grid; gap: .65rem; }
.project-form { margin-block: 1rem 1.5rem; }
.member-create { display: grid; grid-template-columns: minmax(12rem, 1fr) 10rem auto; gap: .75rem; margin-block: 1rem; }
@media (max-width: 700px) { .member-create { grid-template-columns: 1fr; } }
</style>
