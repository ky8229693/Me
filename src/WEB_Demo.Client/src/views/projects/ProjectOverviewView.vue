<script setup lang="ts">
import { computed, inject, ref, watch } from 'vue';
import { RouterLink } from 'vue-router';
import Button from 'primevue/button';
import Message from 'primevue/message';
import ProgressBar from 'primevue/progressbar';
import ProgressSpinner from 'primevue/progressspinner';
import Tag from 'primevue/tag';
import { getProjectOverview } from '../../api/projects';
import type { ProjectOverviewInfo, ProjectOverviewIssueInfo } from '../../api/types';
import { projectWorkspaceKey } from '../../features/projects/projectContext';
import { useI18n } from 'vue-i18n';
import { useDomainLabels } from '../../i18n/domain';

const context = inject(projectWorkspaceKey)!;
const { t, locale } = useI18n();
const { issueStatusLabel, priorityLabel, projectStatusLabel, projectRoleLabel } = useDomainLabels();
const summary = ref<ProjectOverviewInfo | null>(null);
const loading = ref(true);
const error = ref('');
const projectId = computed(() => context.project.value.id);

async function load() {
  loading.value = true;
  error.value = '';
  summary.value = null;
  try {
    const result = await getProjectOverview(projectId.value);
    if (!result) throw new Error(t('projects.overviewLoadFailed'));
    summary.value = result;
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : t('projects.overviewLoadFailed');
  } finally {
    loading.value = false;
  }
}

watch(projectId, load, { immediate: true });

const kpis = computed(() => summary.value ? [
  { key: 'total', label: t('projects.totalIssues'), value: summary.value.totalCount, icon: 'pi-list-check' },
  { key: 'progress', label: t('projects.inProgressIssues'), value: summary.value.inProgressCount, icon: 'pi-spinner' },
  { key: 'completed', label: t('projects.completedIssues'), value: summary.value.completedCount, icon: 'pi-check-circle' },
  { key: 'overdue', label: t('projects.overdueIssues'), value: summary.value.overdueCount, icon: 'pi-clock' }
] : []);

function formatDate(value: string) {
  return new Intl.DateTimeFormat(locale.value, { year: 'numeric', month: 'short', day: 'numeric' }).format(new Date(value));
}
function issueTarget(issue: ProjectOverviewIssueInfo) {
  return { name: 'project-issue-detail', params: { projectId: projectId.value, issueId: issue.id }, query: { from: `/projects/${projectId.value}/overview` } };
}
</script>

<template>
  <section class="workspace-panel project-dashboard">
    <header class="section-heading"><div><h2>{{ t('projects.overview') }}</h2><p>{{ t('projects.overviewSubtitle') }}</p></div></header>
    <div v-if="loading" class="overview-state" role="status"><ProgressSpinner :aria-label="t('projects.overviewLoading')"/><p>{{ t('projects.overviewLoading') }}</p></div>
    <Message v-else-if="error" severity="error" :closable="false"><div class="error-content"><span>{{ error }}</span><Button :label="t('common.retry')" size="small" @click="load"/></div></Message>
    <template v-else-if="summary">
      <div class="kpi-grid">
        <article v-for="item in kpis" :key="item.key" class="kpi-card" :class="`kpi-${item.key}`">
          <div><span>{{ item.label }}</span><strong>{{ item.value }}</strong></div><i class="pi" :class="item.icon"></i>
        </article>
      </div>

      <div class="dashboard-grid">
        <article class="dashboard-card completion-card">
          <div class="card-heading"><div><h3>{{ t('projects.completion') }}</h3><small>{{ t('projects.completedOfTotal', { completed: summary.completedCount, total: summary.totalCount }) }}</small></div><strong>{{ summary.completionRate }}%</strong></div>
          <ProgressBar :value="summary.completionRate" :show-value="false"/>
        </article>
        <article class="dashboard-card">
          <h3>{{ t('projects.projectDetails') }}</h3>
          <dl class="project-facts"><div><dt>{{ t('common.status') }}</dt><dd>{{ projectStatusLabel(context.project.value.status) }}</dd></div><div><dt>{{ t('projects.yourRole') }}</dt><dd>{{ projectRoleLabel(context.project.value.currentUserRole) }}</dd></div><div><dt>{{ t('projects.overviewMembers') }}</dt><dd>{{ context.members.value.length }}</dd></div></dl>
        </article>
      </div>

      <div class="dashboard-grid">
        <article class="dashboard-card status-card">
          <h3>{{ t('projects.statusDistribution') }}</h3>
          <div class="status-list">
            <div v-for="item in summary.statusCounts" :key="item.status" class="status-row">
              <div><span>{{ issueStatusLabel(item.status) }}</span><strong>{{ item.count }}</strong></div>
              <div class="status-track"><span :style="{ width: `${summary.totalCount ? item.count / summary.totalCount * 100 : 0}%` }"></span></div>
            </div>
          </div>
        </article>
        <article class="dashboard-card">
          <h3>{{ t('projects.quickActions') }}</h3>
          <div class="quick-links"><RouterLink :to="{ name: 'project-issues' }"><i class="pi pi-list-check"></i><span><strong>{{ t('projects.issues') }}</strong><small>{{ t('projects.quickIssues') }}</small></span></RouterLink><RouterLink :to="{ name: 'project-members' }"><i class="pi pi-users"></i><span><strong>{{ t('projects.members') }}</strong><small>{{ t('projects.quickMembers') }}</small></span></RouterLink></div>
        </article>
      </div>

      <div class="dashboard-grid issue-sections">
        <article class="dashboard-card issue-summary">
          <h3>{{ t('projects.recentIssues') }}</h3>
          <div v-if="summary.recentIssues.length" class="issue-links"><RouterLink v-for="issue in summary.recentIssues" :key="issue.id" :to="issueTarget(issue)"><span><strong>{{ issue.title }}</strong><small>{{ t('projects.updatedOn', { date: formatDate(issue.updatedAt) }) }}</small></span><Tag :value="issueStatusLabel(issue.status)" severity="secondary"/></RouterLink></div>
          <p v-else class="empty-text">{{ t('projects.noRecentIssues') }}</p>
        </article>
        <article class="dashboard-card issue-summary">
          <h3>{{ t('projects.upcomingIssues') }}</h3>
          <div v-if="summary.upcomingIssues.length" class="issue-links"><RouterLink v-for="issue in summary.upcomingIssues" :key="issue.id" :to="issueTarget(issue)"><span><strong>{{ issue.title }}</strong><small>{{ t('common.due', { date: formatDate(issue.dueDate!) }) }}</small></span><Tag :value="priorityLabel(issue.priority)" severity="secondary"/></RouterLink></div>
          <p v-else class="empty-text">{{ t('projects.noUpcomingIssues') }}</p>
        </article>
      </div>
    </template>
  </section>
</template>

<style scoped>
.project-dashboard{display:grid;gap:1rem;min-width:0}.section-heading h2,.section-heading p,.dashboard-card h3{margin:0}.section-heading p,.empty-text,small,dt{color:var(--p-text-muted-color)}.overview-state{min-height:16rem;display:grid;place-content:center;justify-items:center}.error-content{display:flex;align-items:center;justify-content:space-between;gap:1rem}.kpi-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:1rem}.kpi-card,.dashboard-card{border:1px solid var(--p-content-border-color);border-radius:var(--p-border-radius);background:var(--p-content-background);padding:1rem;min-width:0}.kpi-card{display:flex;align-items:center;justify-content:space-between;gap:1rem}.kpi-card div{display:grid;gap:.35rem}.kpi-card strong{font-size:1.8rem}.kpi-card>i{display:grid;place-content:center;width:2.75rem;height:2.75rem;border-radius:50%;background:var(--p-primary-50);color:var(--p-primary-color);font-size:1.2rem}.kpi-overdue>i{background:var(--p-red-50);color:var(--p-red-500)}.dashboard-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:1rem}.dashboard-card{display:grid;align-content:start;gap:1rem}.card-heading{display:flex;align-items:flex-start;justify-content:space-between;gap:1rem}.card-heading>strong{font-size:1.5rem;color:var(--p-primary-color)}.project-facts{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:1rem;margin:0}.project-facts div{display:grid;gap:.25rem}.project-facts dd{margin:0;font-weight:600}.status-list{display:grid;gap:.8rem}.status-row{display:grid;gap:.35rem}.status-row>div:first-child{display:flex;justify-content:space-between}.status-track{height:.5rem;border-radius:999px;background:var(--p-surface-100);overflow:hidden}.status-track span{display:block;height:100%;border-radius:inherit;background:var(--p-primary-color)}.quick-links{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:.75rem}.quick-links a,.issue-links a{border:1px solid var(--p-content-border-color);border-radius:var(--p-border-radius);text-decoration:none;color:inherit}.quick-links a{display:grid;grid-template-columns:auto 1fr;gap:.7rem;padding:.85rem}.quick-links span,.issue-links span{display:grid;gap:.2rem}.quick-links i{color:var(--p-primary-color);font-size:1.25rem}.issue-links{display:grid;gap:.65rem}.issue-links a{display:flex;align-items:center;justify-content:space-between;gap:1rem;padding:.75rem}.issue-links a:hover,.quick-links a:hover{border-color:var(--p-primary-color);background:var(--p-primary-50)}.issue-links strong{overflow-wrap:anywhere}.empty-text{margin:0}.p-progressbar{height:.7rem}
@media(max-width:900px){.kpi-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.dashboard-grid{grid-template-columns:1fr}}
@media(max-width:550px){.kpi-grid,.quick-links,.project-facts{grid-template-columns:1fr}.error-content,.issue-links a{align-items:flex-start;flex-direction:column}}
</style>
