<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import Button from 'primevue/button';
import Column from 'primevue/column';
import DataTable from 'primevue/datatable';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Message from 'primevue/message';
import Tag from 'primevue/tag';
import Textarea from 'primevue/textarea';
import { useToast } from 'primevue/usetoast';
import type { AuthUser, ProjectInfo } from '../api/types';
import { createProject, getProjects } from '../api/projects';
import { hasPermission, permissions } from '../auth/permissions';

const props = defineProps<{ currentUser: AuthUser }>();
const router = useRouter();
const toast = useToast();
const projects = ref<ProjectInfo[]>([]);
const loading = ref(false);
const errorMessage = ref('');
const createVisible = ref(false);
const createName = ref('');
const createDescription = ref('');
const canCreate = () => hasPermission(props.currentUser, permissions.projectsCreate);

async function loadProjects() {
  loading.value = true; errorMessage.value = '';
  try { projects.value = (await getProjects()) ?? []; }
  catch (error) { errorMessage.value = error instanceof Error ? error.message : 'Unable to load projects.'; }
  finally { loading.value = false; }
}

async function submitProject() {
  try {
    const created = await createProject(createName.value, createDescription.value.trim() || null);
    createVisible.value = false; createName.value = ''; createDescription.value = '';
    if (created) await router.push({ name: 'project-overview', params: { projectId: created.id } });
  } catch (error) { toast.add({ severity: 'error', detail: error instanceof Error ? error.message : 'Unable to create project.', life: 3600 }); }
}

function openProject(project: ProjectInfo) { void router.push({ name: 'project-overview', params: { projectId: project.id } }); }
onMounted(loadProjects);
</script>

<template><section class="workspace projects-view">
  <header class="page-header"><div><p class="eyebrow">Project Management</p><h1>Projects</h1><p>Choose a project to open its workspace.</p></div><Button v-if="canCreate()" label="Create project" icon="pi pi-plus" @click="createVisible = true" /></header>
  <Message v-if="errorMessage" severity="error" class="content-alert">{{ errorMessage }}</Message>
  <DataTable :value="projects" :loading="loading" data-key="id" striped-rows @row-click="openProject($event.data)">
    <template #empty>No accessible projects.</template><Column field="name" header="Name" /><Column field="description" header="Description" /><Column field="currentUserRole" header="Your role" /><Column header="Status"><template #body="{ data }"><Tag :value="data.status" :severity="data.status === 'Active' ? 'success' : 'secondary'" /></template></Column><Column header="Actions"><template #body="{ data }"><Button label="Open workspace" size="small" severity="secondary" @click.stop="openProject(data)" /></template></Column>
  </DataTable>
  <Dialog v-model:visible="createVisible" modal header="Create project" :style="{ width: '32rem' }"><div class="project-form"><label>Name<InputText v-model="createName" autofocus /></label><label>Description<Textarea v-model="createDescription" rows="4" /></label></div><template #footer><Button label="Cancel" severity="secondary" @click="createVisible = false" /><Button label="Create" :disabled="createName.trim().length < 2" @click="submitProject" /></template></Dialog>
</section></template>
<style scoped>.project-form,.project-form label{display:grid;gap:.65rem}</style>
