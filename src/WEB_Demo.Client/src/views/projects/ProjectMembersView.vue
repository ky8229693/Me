<script setup lang="ts">
import { computed, inject, ref } from 'vue';
import AutoComplete from 'primevue/autocomplete';
import Button from 'primevue/button';
import Column from 'primevue/column';
import DataTable from 'primevue/datatable';
import Select from 'primevue/select';
import Tag from 'primevue/tag';
import { useToast } from 'primevue/usetoast';
import type { ProjectMemberInfo } from '../../api/types';
import { addProjectMember, projectRoles, removeProjectMember, searchProjectMemberCandidates, updateProjectMemberRole } from '../../api/projects';
import { projectWorkspaceKey } from '../../features/projects/projectContext';
import { useI18n } from 'vue-i18n';
import { useDomainLabels } from '../../i18n/domain';

const context = inject(projectWorkspaceKey)!;
const toast = useToast();
const { t } = useI18n();
const { projectRoleLabel } = useDomainLabels();
const email = ref('');
const suggestions = ref<string[]>([]);
const role = ref<(typeof projectRoles)[number]>('Member');
const roleDrafts = ref<Record<string, string>>({});
const canManage = computed(() => context.project.value.status === 'Active' && ['Owner', 'Manager'].includes(context.project.value.currentUserRole));
const isOwner = computed(() => context.project.value.currentUserRole === 'Owner');
const roles = computed(() => (isOwner.value ? [...projectRoles] : ['Member', 'Viewer']).map(value => ({ value, label: projectRoleLabel(value) })));

async function search(event: { query: string }) {
  if (event.query.trim().length < 2) { suggestions.value = []; return; }
  try { suggestions.value = (await searchProjectMemberCandidates(context.project.value.id, event.query.trim()))?.map(item => item.email) ?? []; }
  catch (cause) { notify(cause, t('common.error')); }
}
async function add() {
  try { await addProjectMember(context.project.value.id, email.value.trim(), role.value); email.value = ''; await context.reloadMembers(); }
  catch (cause) { notify(cause, t('common.error')); }
}
async function save(item: ProjectMemberInfo) {
  try { await updateProjectMemberRole(context.project.value.id, item.userId, roleDrafts.value[item.userId] ?? item.role); await context.reloadMembers(); }
  catch (cause) { notify(cause, t('common.error')); }
}
async function remove(item: ProjectMemberInfo) {
  if (!window.confirm(`${t('common.remove')} ${item.email}?`)) return;
  try { await removeProjectMember(context.project.value.id, item.userId); await context.reloadMembers(); }
  catch (cause) { notify(cause, t('common.error')); }
}
function manageable(item: ProjectMemberInfo) { return isOwner.value || ['Member', 'Viewer'].includes(item.role); }
function notify(cause: unknown, fallback: string) { toast.add({ severity: 'error', detail: cause instanceof Error ? cause.message : fallback, life: 3600 }); }
</script>

<template><section class="workspace-panel"><header class="section-header"><div><h2>{{t('projects.members')}}</h2><p>{{t('projects.memberAccess')}}</p></div></header>
  <div v-if="canManage" class="member-create"><AutoComplete v-model="email" :suggestions="suggestions" :min-length="2" :delay="250" :placeholder="t('common.email')" :aria-label="t('common.email')" @complete="search" /><Select v-model="role" :options="roles" option-label="label" option-value="value" :aria-label="t('projects.projectRole')" /><Button :label="t('projects.addMember')" :disabled="!email.trim()" @click="add" /></div>
  <DataTable :value="context.members.value" data-key="userId" striped-rows><template #empty>{{t('projects.noMembers')}}</template><Column field="email" :header="t('projects.user')" /><Column :header="t('projects.projectRole')"><template #body="{ data }"><Select v-if="canManage && manageable(data)" v-model="roleDrafts[data.userId]" :options="roles" option-label="label" option-value="value" :placeholder="projectRoleLabel(data.role)" :aria-label="`${t('projects.projectRole')} ${data.email}`" /><Tag v-else :value="projectRoleLabel(data.role)" severity="secondary" /></template></Column><Column :header="t('common.actions')"><template #body="{ data }"><div class="row-actions"><Button :label="t('projects.saveRole')" size="small" :disabled="!canManage || !manageable(data) || !roleDrafts[data.userId] || roleDrafts[data.userId] === data.role" @click="save(data)" /><Button :label="t('common.remove')" size="small" severity="danger" :disabled="!canManage || !manageable(data)" @click="remove(data)" /></div></template></Column></DataTable>
</section></template>
<style scoped>.section-header h2,.section-header p{margin:.2rem 0}.member-create{display:grid;grid-template-columns:minmax(12rem,1fr) 10rem auto;gap:.75rem;margin:1rem 0}@media(max-width:700px){.member-create{grid-template-columns:1fr}}</style>
