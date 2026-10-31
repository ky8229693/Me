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
import { useI18n } from 'vue-i18n';
import { useDomainLabels } from '../i18n/domain';

const props = defineProps<{ currentUser: AuthUser }>();
const router = useRouter();
const toast = useToast();
const { t } = useI18n();
const { projectStatusLabel, projectRoleLabel } = useDomainLabels();
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
  catch (error) { errorMessage.value = error instanceof Error ? error.message : t('projects.loadFailed'); }
  finally { loading.value = false; }
}

async function submitProject() {
  try {
    const created = await createProject(createName.value, createDescription.value.trim() || null);
    createVisible.value = false; createName.value = ''; createDescription.value = '';
    if (created) await router.push({ name: 'project-overview', params: { projectId: created.id } });
  } catch (error) { toast.add({ severity: 'error', detail: error instanceof Error ? error.message : t('projects.createFailed'), life: 3600 }); }
}

function openProject(project: ProjectInfo) { void router.push({ name: 'project-overview', params: { projectId: project.id } }); }
onMounted(loadProjects);
</script>

<template><section class="workspace projects-view">
  <header class="page-header"><div><p class="eyebrow">{{t('projects.eyebrow')}}</p><h1>{{t('projects.title')}}</h1><p>{{t('projects.subtitle')}}</p></div><Button v-if="canCreate()" :label="t('projects.create')" icon="pi pi-plus" @click="createVisible = true" /></header>
  <Message v-if="errorMessage" severity="error" class="content-alert">{{ errorMessage }}</Message>
  <DataTable :value="projects" :loading="loading" data-key="id" striped-rows @row-click="openProject($event.data)">
    <template #empty>{{t('projects.empty')}}</template><Column field="name" :header="t('common.name')" /><Column field="description" :header="t('common.description')" /><Column :header="t('projects.yourRole')"><template #body="{data}">{{projectRoleLabel(data.currentUserRole)}}</template></Column><Column :header="t('common.status')"><template #body="{ data }"><Tag :value="projectStatusLabel(data.status)" :severity="data.status === 'Active' ? 'success' : 'secondary'" /></template></Column><Column :header="t('common.actions')"><template #body="{ data }"><Button :label="t('projects.openWorkspace')" size="small" severity="secondary" @click.stop="openProject(data)" /></template></Column>
  </DataTable>
  <Dialog v-model:visible="createVisible" modal :header="t('projects.create')" :style="{ width: '32rem' }"><div class="project-form"><label>{{t('common.name')}}<InputText v-model="createName" autofocus /></label><label>{{t('common.description')}}<Textarea v-model="createDescription" rows="4" /></label></div><template #footer><Button :label="t('common.cancel')" severity="secondary" @click="createVisible = false" /><Button :label="t('common.create')" :disabled="createName.trim().length < 2" @click="submitProject" /></template></Dialog>
</section></template>
<style scoped>.project-form,.project-form label{display:grid;gap:.65rem}</style>
