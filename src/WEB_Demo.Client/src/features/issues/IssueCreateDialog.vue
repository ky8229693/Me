<script setup lang="ts">
import { computed, inject, ref, watch } from 'vue';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Textarea from 'primevue/textarea';
import { useToast } from 'primevue/usetoast';
import { createIssue, issuePriorities, issueTypes } from '../../api/issues';
import type { IssueInfo, IssuePriority, IssueType } from '../../api/types';
import { projectWorkspaceKey } from '../projects/projectContext';

const props = defineProps<{ visible: boolean }>();
const emit = defineEmits<{ 'update:visible': [value: boolean]; created: [issue: IssueInfo] }>();
const context = inject(projectWorkspaceKey)!;
const toast = useToast();
const title = ref(''); const description = ref(''); const type = ref<IssueType>('Feature'); const priority = ref<IssuePriority>('Medium'); const assignee = ref<string | null>(null); const dueDate = ref('');
const manager = computed(() => ['Owner', 'Manager'].includes(context.project.value.currentUserRole));
const options = computed(() => context.members.value.filter(item => manager.value || item.userId === context.currentUser.id).map(item => ({ label: item.email, value: item.userId })));
watch(() => props.visible, value => { if (value) { title.value='';description.value='';type.value='Feature';priority.value='Medium';assignee.value=null;dueDate.value=''; } });
async function submit() { try { const created = await createIssue(context.project.value.id, { title:title.value,description:description.value.trim()||null,type:type.value,priority:priority.value,assigneeUserId:assignee.value,dueDate:dueDate.value?new Date(dueDate.value).toISOString():null }); if (created) emit('created', created); emit('update:visible', false); } catch(cause){toast.add({severity:'error',detail:cause instanceof Error?cause.message:'Unable to create Issue.',life:3600});} }
</script>
<template><Dialog :visible="visible" modal header="Create Issue" :style="{width:'min(42rem,94vw)'}" @update:visible="emit('update:visible',$event)"><div class="issue-form"><label>Title<InputText v-model="title" autofocus /></label><label>Description<Textarea v-model="description" rows="5" /></label><div class="form-row"><label>Type<Select v-model="type" :options="issueTypes" /></label><label>Priority<Select v-model="priority" :options="issuePriorities" /></label></div><label>Assignee<Select v-model="assignee" :options="options" option-label="label" option-value="value" show-clear placeholder="Unassigned" /></label><label>Due date<InputText v-model="dueDate" type="datetime-local" /></label></div><template #footer><Button label="Cancel" severity="secondary" @click="emit('update:visible',false)" /><Button label="Create" :disabled="title.trim().length<2" @click="submit" /></template></Dialog></template>
<style scoped>.issue-form,.issue-form label{display:grid;gap:.65rem}.form-row{display:grid;grid-template-columns:1fr 1fr;gap:.75rem}@media(max-width:600px){.form-row{grid-template-columns:1fr}}</style>
