<script setup lang="ts">
import { computed, inject, ref, watchEffect } from 'vue';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Message from 'primevue/message';
import Textarea from 'primevue/textarea';
import { useToast } from 'primevue/usetoast';
import { archiveProject, restoreProject, updateProject } from '../../api/projects';
import { projectWorkspaceKey } from '../../features/projects/projectContext';
import { useI18n } from 'vue-i18n';

const context = inject(projectWorkspaceKey)!;
const toast = useToast();
const { t } = useI18n();
const name = ref(''); const description = ref('');
watchEffect(() => { name.value = context.project.value.name; description.value = context.project.value.description ?? ''; });
const canEdit = computed(() => context.project.value.status === 'Active' && ['Owner', 'Manager'].includes(context.project.value.currentUserRole));
const owner = computed(() => context.project.value.currentUserRole === 'Owner');
async function save() { try { await updateProject(context.project.value.id, name.value, description.value.trim() || null); await context.reloadProject(); toast.add({ severity: 'success', detail: t('projects.updated'), life: 2600 }); } catch (cause) { notify(cause); } }
async function toggleArchive() { try { context.project.value.status === 'Active' ? await archiveProject(context.project.value.id) : await restoreProject(context.project.value.id); await context.reloadProject(); } catch (cause) { notify(cause); } }
function notify(cause: unknown) { toast.add({ severity: 'error', detail: cause instanceof Error ? cause.message : t('projects.updateFailed'), life: 3600 }); }
</script>
<template><section class="workspace-panel"><h2>{{t('projects.settingsTitle')}}</h2><Message v-if="context.project.value.status === 'Archived'" severity="warn">{{t('projects.restoredOnlyOwner')}}</Message><div class="settings-form"><label>{{t('common.name')}}<InputText v-model="name" :disabled="!canEdit" /></label><label>{{t('common.description')}}<Textarea v-model="description" rows="5" :disabled="!canEdit" /></label><div class="row-actions"><Button :label="t('projects.saveChanges')" :disabled="!canEdit || name.trim().length < 2" @click="save" /><Button v-if="owner" :label="t(context.project.value.status === 'Active' ? 'projects.archive' : 'projects.restore')" :severity="context.project.value.status === 'Active' ? 'danger' : 'secondary'" @click="toggleArchive" /></div></div></section></template>
<style scoped>.settings-form,.settings-form label{display:grid;gap:.65rem}.settings-form{max-width:46rem}</style>
