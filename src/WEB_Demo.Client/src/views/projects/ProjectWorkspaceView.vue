<script setup lang="ts">
import { computed, provide, ref, watch } from 'vue';
import { RouterLink, RouterView, useRoute } from 'vue-router';
import Message from 'primevue/message';
import ProgressSpinner from 'primevue/progressspinner';
import Tag from 'primevue/tag';
import type { AuthUser, ProjectInfo, ProjectMemberInfo } from '../../api/types';
import { getProject, getProjectMembers } from '../../api/projects';
import { projectWorkspaceKey } from '../../features/projects/projectContext';

const props = defineProps<{ currentUser: AuthUser }>();
const route = useRoute();
const project = ref<ProjectInfo | null>(null);
const members = ref<ProjectMemberInfo[]>([]);
const loading = ref(true);
const error = ref('');
const projectId = computed(() => String(route.params.projectId));

async function reloadProject() {
  const loaded = await getProject(projectId.value);
  if (!loaded) throw new Error('Project was not found.');
  project.value = loaded;
}
async function reloadMembers() { members.value = (await getProjectMembers(projectId.value)) ?? []; }
async function loadContext() {
  loading.value = true; error.value = ''; project.value = null; members.value = [];
  try { await Promise.all([reloadProject(), reloadMembers()]); }
  catch (cause) { error.value = cause instanceof Error ? cause.message : 'Unable to load project.'; }
  finally { loading.value = false; }
}
watch(projectId, loadContext, { immediate: true });

provide(projectWorkspaceKey, {
  project: computed(() => project.value!) as typeof project & { value: ProjectInfo }, members,
  currentUser: props.currentUser, reloadProject, reloadMembers
});
const canSettings = computed(() => project.value && ['Owner', 'Manager'].includes(project.value.currentUserRole));
</script>

<template><section class="workspace project-workspace">
  <div v-if="loading" class="workspace-state"><ProgressSpinner aria-label="Loading project" /><p>Loading project…</p></div>
  <Message v-else-if="error" severity="error"><strong>Project unavailable.</strong> {{ error }}</Message>
  <template v-else-if="project">
    <nav class="breadcrumbs" aria-label="Breadcrumb"><RouterLink :to="{ name: 'projects' }">Projects</RouterLink><i class="pi pi-angle-right"></i><span>{{ project.name }}</span></nav>
    <header class="project-header"><div><p class="eyebrow">Project Workspace</p><h1>{{ project.name }}</h1><p>{{ project.description || 'No project description.' }}</p></div><div class="project-badges"><Tag :value="project.status" :severity="project.status === 'Active' ? 'success' : 'warn'" /><Tag :value="project.currentUserRole" severity="secondary" /></div></header>
    <Message v-if="project.status === 'Archived'" severity="warn">This project is archived and all project work is read-only.</Message>
    <nav class="workspace-tabs" aria-label="Project navigation"><RouterLink :to="{ name: 'project-overview', params: { projectId } }">Overview</RouterLink><RouterLink :to="{ name: 'project-issues', params: { projectId } }">Issues</RouterLink><RouterLink :to="{ name: 'project-members', params: { projectId } }">Members</RouterLink><RouterLink v-if="canSettings" :to="{ name: 'project-settings', params: { projectId } }">Settings</RouterLink></nav>
    <RouterView />
  </template>
</section></template>

<style scoped>.project-workspace{display:grid;gap:1rem}.workspace-state{min-height:20rem;display:grid;place-content:center;justify-items:center}.breadcrumbs,.project-badges,.workspace-tabs{display:flex;align-items:center;gap:.65rem;flex-wrap:wrap}.breadcrumbs{font-size:.9rem;color:var(--p-text-muted-color)}.project-header{display:flex;justify-content:space-between;align-items:flex-start;gap:1rem}.project-header h1,.project-header p{margin:.2rem 0}.workspace-tabs{border-bottom:1px solid var(--p-content-border-color)}.workspace-tabs a{padding:.8rem .25rem;text-decoration:none;color:var(--p-text-muted-color);border-bottom:2px solid transparent}.workspace-tabs a.router-link-active{color:var(--p-primary-color);border-color:var(--p-primary-color)}@media(max-width:650px){.project-header{display:grid}.workspace-tabs{overflow-x:auto;flex-wrap:nowrap}}</style>
