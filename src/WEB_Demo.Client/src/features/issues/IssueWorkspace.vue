<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import Button from 'primevue/button';
import Column from 'primevue/column';
import DataTable from 'primevue/datatable';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Message from 'primevue/message';
import Select from 'primevue/select';
import TabPanel from 'primevue/tabpanel';
import TabView from 'primevue/tabview';
import Tag from 'primevue/tag';
import Textarea from 'primevue/textarea';
import { useToast } from 'primevue/usetoast';
import {
  addIssueComment, assignIssue, createIssue, getIssueComments, getIssueHistory, getIssues,
  issuePriorities, issueStatuses, issueTypes, transitionIssue, updateIssue
} from '../../api/issues';
import type { AuthUser, IssueCommentInfo, IssueHistoryInfo, IssueInfo, IssuePriority, IssueType, ProjectInfo, ProjectMemberInfo } from '../../api/types';

const props = defineProps<{ project: ProjectInfo; members: ProjectMemberInfo[]; currentUser: AuthUser }>();
const toast = useToast();
const issues = ref<IssueInfo[]>([]);
const total = ref(0);
const page = ref(1);
const pageSize = ref(20);
const loading = ref(false);
const error = ref('');
const search = ref('');
const typeFilter = ref<string | null>(null);
const statusFilter = ref<string | null>(null);
const priorityFilter = ref<string | null>(null);
const assigneeFilter = ref<string | null>(null);
const createVisible = ref(false);
const selected = ref<IssueInfo | null>(null);
const history = ref<IssueHistoryInfo[]>([]);
const comments = ref<IssueCommentInfo[]>([]);
const title = ref('');
const description = ref('');
const type = ref<IssueType>('Feature');
const priority = ref<IssuePriority>('Medium');
const assignee = ref<string | null>(null);
const dueDate = ref('');
const comment = ref('');

const active = computed(() => props.project.status === 'Active');
const manager = computed(() => ['Owner', 'Manager'].includes(props.project.currentUserRole));
const canCreate = computed(() => active.value && props.project.currentUserRole !== 'Viewer');
const assigneeOptions = computed(() => props.members.map(item => ({ label: item.email, value: item.userId })));
const createAssigneeOptions = computed(() => manager.value
  ? assigneeOptions.value
  : assigneeOptions.value.filter(item => item.value === props.currentUser.id));

async function loadIssues() {
  loading.value = true;
  error.value = '';
  try {
    const result = await getIssues(props.project.id, {
      search: search.value.trim(), type: typeFilter.value, status: statusFilter.value,
      priority: priorityFilter.value, assigneeUserId: assigneeFilter.value,
      page: page.value, pageSize: pageSize.value
    });
    issues.value = result?.items ?? [];
    total.value = result?.totalCount ?? 0;
  } catch (cause) { error.value = cause instanceof Error ? cause.message : 'Unable to load Issues.'; }
  finally { loading.value = false; }
}

function resetForm(issue?: IssueInfo) {
  title.value = issue?.title ?? '';
  description.value = issue?.description ?? '';
  type.value = issue?.type ?? 'Feature';
  priority.value = issue?.priority ?? 'Medium';
  assignee.value = issue?.assigneeUserId ?? null;
  dueDate.value = issue?.dueDate ? issue.dueDate.slice(0, 16) : '';
}

async function openIssue(issue: IssueInfo) {
  selected.value = issue;
  resetForm(issue);
  try {
    [history.value, comments.value] = await Promise.all([
      getIssueHistory(props.project.id, issue.id).then(value => value ?? []),
      getIssueComments(props.project.id, issue.id).then(value => value ?? [])
    ]);
  } catch (cause) { notifyError(cause, 'Unable to load Issue details.'); }
}

async function submitCreate() {
  try {
    const created = await createIssue(props.project.id, {
      title: title.value, description: description.value.trim() || null, type: type.value,
      priority: priority.value, assigneeUserId: assignee.value,
      dueDate: dueDate.value ? new Date(dueDate.value).toISOString() : null
    });
    createVisible.value = false;
    resetForm();
    await loadIssues();
    if (created) await openIssue(created);
    toast.add({ severity: 'success', detail: 'Issue created.', life: 2600 });
  } catch (cause) { notifyError(cause, 'Unable to create Issue.'); }
}

async function saveIssue() {
  if (!selected.value) return;
  try {
    let updated = await updateIssue(props.project.id, selected.value.id, {
      title: title.value, description: description.value.trim() || null, type: type.value,
      priority: priority.value, dueDate: dueDate.value ? new Date(dueDate.value).toISOString() : null
    });
    if (selected.value.canAssign && assignee.value !== selected.value.assigneeUserId)
      updated = await assignIssue(props.project.id, selected.value.id, assignee.value);
    if (updated) await refreshSelected(updated);
    toast.add({ severity: 'success', detail: 'Issue updated.', life: 2600 });
  } catch (cause) { notifyError(cause, 'Unable to update Issue.'); }
}

async function moveTo(status: string) {
  if (!selected.value) return;
  try {
    const updated = await transitionIssue(props.project.id, selected.value.id, status);
    if (updated) await refreshSelected(updated);
    toast.add({ severity: 'success', detail: `Issue moved to ${status}.`, life: 2600 });
  } catch (cause) { notifyError(cause, 'Unable to change Issue status.'); }
}

async function submitComment() {
  if (!selected.value || !comment.value.trim()) return;
  try {
    const created = await addIssueComment(props.project.id, selected.value.id, comment.value);
    if (created) comments.value.push(created);
    comment.value = '';
  } catch (cause) { notifyError(cause, 'Unable to add comment.'); }
}

async function refreshSelected(updated: IssueInfo) {
  selected.value = updated;
  resetForm(updated);
  await Promise.all([loadIssues(), getIssueHistory(props.project.id, updated.id).then(value => history.value = value ?? [])]);
}

function changePage(event: { page: number; rows: number }) {
  page.value = event.page + 1;
  pageSize.value = event.rows;
  void loadIssues();
}

function applyFilters() { page.value = 1; void loadIssues(); }
function notifyError(cause: unknown, fallback: string) {
  toast.add({ severity: 'error', detail: cause instanceof Error ? cause.message : fallback, life: 3600 });
}

watch(() => props.project.id, () => { selected.value = null; page.value = 1; void loadIssues(); });
onMounted(loadIssues);
</script>

<template>
  <section class="issues-workspace">
    <div class="section-heading"><h2>Issues</h2><Button v-if="canCreate" label="Create Issue" icon="pi pi-plus" size="small" @click="resetForm(); createVisible = true" /></div>
    <Message v-if="error" severity="error">{{ error }}</Message>
    <div class="filters">
      <InputText v-model="search" placeholder="Search title or description" @keyup.enter="applyFilters" />
      <Select v-model="typeFilter" :options="issueTypes" show-clear placeholder="Type" />
      <Select v-model="statusFilter" :options="issueStatuses" show-clear placeholder="Status" />
      <Select v-model="priorityFilter" :options="issuePriorities" show-clear placeholder="Priority" />
      <Select v-model="assigneeFilter" :options="assigneeOptions" option-label="label" option-value="value" show-clear placeholder="Assignee" />
      <Button label="Apply" severity="secondary" @click="applyFilters" />
    </div>
    <DataTable :value="issues" :loading="loading" data-key="id" lazy paginator :rows="pageSize" :first="(page - 1) * pageSize" :total-records="total" :rows-per-page-options="[10, 20, 50, 100]" @page="changePage" @row-click="openIssue($event.data)">
      <template #empty>No matching Issues.</template>
      <Column field="title" header="Title" />
      <Column field="type" header="Type" />
      <Column field="priority" header="Priority" />
      <Column header="Status"><template #body="{ data }"><Tag :value="data.status" /></template></Column>
      <Column field="assigneeEmail" header="Assignee"><template #body="{ data }">{{ data.assigneeEmail || 'Unassigned' }}</template></Column>
      <Column header="Actions"><template #body="{ data }"><Button label="Open" size="small" severity="secondary" @click.stop="openIssue(data)" /></template></Column>
    </DataTable>

    <Dialog v-model:visible="createVisible" modal header="Create Issue" :style="{ width: 'min(42rem, 94vw)' }">
      <div class="issue-form"><label>Title<InputText v-model="title" autofocus /></label><label>Description<Textarea v-model="description" rows="5" /></label><div class="form-row"><label>Type<Select v-model="type" :options="issueTypes" /></label><label>Priority<Select v-model="priority" :options="issuePriorities" /></label></div><label>Assignee<Select v-model="assignee" :options="createAssigneeOptions" option-label="label" option-value="value" show-clear placeholder="Unassigned" /></label><label>Due date<InputText v-model="dueDate" type="datetime-local" /></label></div>
      <template #footer><Button label="Cancel" severity="secondary" @click="createVisible = false" /><Button label="Create" :disabled="title.trim().length < 2" @click="submitCreate" /></template>
    </Dialog>

    <Dialog :visible="Boolean(selected)" modal :header="selected?.title ?? 'Issue'" :style="{ width: 'min(58rem, 95vw)' }" @update:visible="selected = null">
      <Message v-if="!active" severity="warn">Archived project Issues are read-only.</Message>
      <div v-if="selected" class="status-actions"><Tag :value="selected.status" /><Button v-for="next in selected.allowedTransitions" :key="next" :label="`Move to ${next}`" size="small" @click="moveTo(next)" /></div>
      <TabView v-if="selected" value="0">
        <TabPanel value="0" header="Details"><div class="issue-form"><label>Title<InputText v-model="title" :disabled="!selected.canEdit" /></label><label>Description<Textarea v-model="description" rows="5" :disabled="!selected.canEdit" /></label><div class="form-row"><label>Type<Select v-model="type" :options="issueTypes" :disabled="!selected.canEdit" /></label><label>Priority<Select v-model="priority" :options="issuePriorities" :disabled="!selected.canEdit" /></label></div><label>Assignee<Select v-model="assignee" :options="assigneeOptions" option-label="label" option-value="value" show-clear :disabled="!selected.canAssign" /></label><label>Due date<InputText v-model="dueDate" type="datetime-local" :disabled="!selected.canEdit" /></label><Button label="Save" :disabled="!selected.canEdit && !selected.canAssign" @click="saveIssue" /></div></TabPanel>
        <TabPanel value="1" header="Comments"><div class="timeline"><article v-for="item in comments" :key="item.id"><strong>{{ item.authorEmail }}</strong><small>{{ new Date(item.createdAt).toLocaleString() }}</small><p>{{ item.content }}</p></article><p v-if="!comments.length">No comments.</p></div><div v-if="selected.canComment" class="comment-form"><Textarea v-model="comment" rows="3" placeholder="Add a comment" /><Button label="Comment" :disabled="!comment.trim()" @click="submitComment" /></div></TabPanel>
        <TabPanel value="2" header="History"><div class="timeline"><article v-for="item in history" :key="item.id"><strong>{{ item.actorEmail }} · {{ item.changeType }}</strong><small>{{ new Date(item.createdAt).toLocaleString() }}</small><p>{{ item.fieldName || 'Issue' }}: {{ item.previousValue ?? '—' }} → {{ item.newValue ?? '—' }}</p></article><p v-if="!history.length">No history.</p></div></TabPanel>
      </TabView>
    </Dialog>
  </section>
</template>

<style scoped>
.issues-workspace { margin-top: 2rem; border-top: 1px solid var(--p-content-border-color); padding-top: 1rem; }
.section-heading, .status-actions { display: flex; align-items: center; justify-content: space-between; gap: .75rem; flex-wrap: wrap; }
.filters { display: grid; grid-template-columns: minmax(12rem, 2fr) repeat(4, minmax(8rem, 1fr)) auto; gap: .6rem; margin: 1rem 0; }
.issue-form, .issue-form label, .timeline { display: grid; gap: .65rem; }
.form-row { display: grid; grid-template-columns: 1fr 1fr; gap: .75rem; }
.timeline article { border-bottom: 1px solid var(--p-content-border-color); padding: .6rem 0; }
.timeline small { color: var(--p-text-muted-color); }
.timeline p { margin: .3rem 0; white-space: pre-wrap; }
.comment-form { display: grid; gap: .6rem; margin-top: 1rem; }
@media (max-width: 850px) { .filters, .form-row { grid-template-columns: 1fr; } }
</style>
