<script setup lang="ts">
import { computed, provide, ref, watch } from 'vue';
import { RouterLink, RouterView, useRoute } from 'vue-router';
import Message from 'primevue/message';
import ProgressSpinner from 'primevue/progressspinner';
import Tag from 'primevue/tag';
import type { AuthUser, ProjectInfo, ProjectMemberInfo } from '../../api/types';
import { getProject, getProjectMembers } from '../../api/projects';
import { projectWorkspaceKey } from '../../features/projects/projectContext';
import { useI18n } from 'vue-i18n';
import { useDomainLabels } from '../../i18n/domain';

const props = defineProps<{ currentUser: AuthUser }>();
const route = useRoute();
const { t } = useI18n();
const { projectStatusLabel, projectRoleLabel } = useDomainLabels();
const project = ref<ProjectInfo | null>(null);
const members = ref<ProjectMemberInfo[]>([]);
const loading = ref(true);
const error = ref('');
const projectId = computed(() => String(route.params.projectId));

async function reloadProject() {
  const loaded = await getProject(projectId.value);
  if (!loaded) throw new Error(t('projects.inaccessible'));
  project.value = loaded;
}
async function reloadMembers() { members.value = (await getProjectMembers(projectId.value)) ?? []; }
async function loadContext() {
  loading.value = true; error.value = ''; project.value = null; members.value = [];
  try { await Promise.all([reloadProject(), reloadMembers()]); }
  catch (cause) { error.value = cause instanceof Error ? cause.message : t('projects.loadFailed'); }
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
  <div v-if="loading" class="workspace-state"><ProgressSpinner :aria-label="t('projects.loading')" /><p>{{t('projects.loading')}}</p></div>
  <Message v-else-if="error" severity="error">{{ error }}</Message>
  <template v-else-if="project">
    <nav class="breadcrumbs" aria-label="Breadcrumb"><RouterLink :to="{ name: 'projects' }">{{t('projects.title')}}</RouterLink><i class="pi pi-angle-right"></i><span>{{ project.name }}</span></nav>
    <header class="project-header"><div><p class="eyebrow">{{t('projects.workspace')}}</p><h1>{{ project.name }}</h1><p>{{ project.description || t('projects.noDescription') }}</p></div><div class="project-badges"><Tag :value="projectStatusLabel(project.status)" :severity="project.status === 'Active' ? 'success' : 'warn'" /><Tag :value="projectRoleLabel(project.currentUserRole)" severity="secondary" /></div></header>
    <Message v-if="project.status === 'Archived'" severity="warn">{{t('projects.archivedReadOnly')}}</Message>
    <nav class="workspace-tabs" aria-label="Project navigation"><RouterLink :to="{ name: 'project-overview', params: { projectId } }">{{t('projects.overview')}}</RouterLink><RouterLink :to="{ name: 'project-issues', params: { projectId } }">{{t('projects.issues')}}</RouterLink><RouterLink :to="{ name: 'project-members', params: { projectId } }">{{t('projects.members')}}</RouterLink><RouterLink v-if="canSettings" :to="{ name: 'project-settings', params: { projectId } }">{{t('projects.settings')}}</RouterLink></nav>
    <RouterView />
  </template>
</section></template>

<style scoped>.project-workspace{display:grid;gap:1rem;min-width:0}.project-workspace>*{min-width:0}.workspace-state{min-height:20rem;display:grid;place-content:center;justify-items:center}.breadcrumbs,.project-badges,.workspace-tabs{display:flex;align-items:center;gap:.65rem;flex-wrap:wrap}.breadcrumbs{font-size:.9rem;color:var(--p-text-muted-color)}.project-header{display:flex;justify-content:space-between;align-items:flex-start;gap:1rem}.project-header h1,.project-header p{margin:.2rem 0}.workspace-tabs{border-bottom:1px solid var(--p-content-border-color)}.workspace-tabs a{padding:.8rem .25rem;text-decoration:none;color:var(--p-text-muted-color);border-bottom:2px solid transparent}.workspace-tabs a.router-link-active{color:var(--p-primary-color);border-color:var(--p-primary-color)}@media(max-width:650px){.project-header{display:grid}.workspace-tabs{overflow-x:auto;flex-wrap:nowrap}}</style>
