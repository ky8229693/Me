<script setup lang="ts">
import { computed, inject, ref, watchEffect } from 'vue';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Message from 'primevue/message';
import Textarea from 'primevue/textarea';
import { useToast } from 'primevue/usetoast';
import { archiveProject, restoreProject, updateProject } from '../../api/projects';
import { projectWorkspaceKey } from '../../features/projects/projectContext';

const context = inject(projectWorkspaceKey)!;
const toast = useToast();
const name = ref(''); const description = ref('');
watchEffect(() => { name.value = context.project.value.name; description.value = context.project.value.description ?? ''; });
const canEdit = computed(() => context.project.value.status === 'Active' && ['Owner', 'Manager'].includes(context.project.value.currentUserRole));
const owner = computed(() => context.project.value.currentUserRole === 'Owner');
async function save() { try { await updateProject(context.project.value.id, name.value, description.value.trim() || null); await context.reloadProject(); toast.add({ severity: 'success', detail: 'Project updated.', life: 2600 }); } catch (cause) { notify(cause); } }
async function toggleArchive() { try { context.project.value.status === 'Active' ? await archiveProject(context.project.value.id) : await restoreProject(context.project.value.id); await context.reloadProject(); } catch (cause) { notify(cause); } }
function notify(cause: unknown) { toast.add({ severity: 'error', detail: cause instanceof Error ? cause.message : 'Unable to update project.', life: 3600 }); }
</script>
<template><section class="workspace-panel"><h2>Project settings</h2><Message v-if="context.project.value.status === 'Archived'" severity="warn">Only an Owner can restore this archived project.</Message><div class="settings-form"><label>Name<InputText v-model="name" :disabled="!canEdit" /></label><label>Description<Textarea v-model="description" rows="5" :disabled="!canEdit" /></label><div class="row-actions"><Button label="Save changes" :disabled="!canEdit || name.trim().length < 2" @click="save" /><Button v-if="owner" :label="context.project.value.status === 'Active' ? 'Archive project' : 'Restore project'" :severity="context.project.value.status === 'Active' ? 'danger' : 'secondary'" @click="toggleArchive" /></div></div></section></template>
<style scoped>.settings-form,.settings-form label{display:grid;gap:.65rem}.settings-form{max-width:46rem}</style>
