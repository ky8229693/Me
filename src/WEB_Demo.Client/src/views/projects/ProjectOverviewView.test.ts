import { afterEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import { ref } from 'vue';
import PrimeVue from 'primevue/config';
import Aura from '@primeuix/themes/aura';
import ProjectOverviewView from './ProjectOverviewView.vue';
import { projectWorkspaceKey } from '../../features/projects/projectContext';
import { i18n } from '../../i18n';
import * as projectsApi from '../../api/projects';
import type { ProjectOverviewInfo } from '../../api/types';
import { createTestRouter } from '../../test/router';

vi.mock('../../api/projects', () => ({ getProjectOverview: vi.fn() }));

const wrappers: VueWrapper[] = [];
const project = {
  id: 'project-1', name: 'PMS', description: 'Core', status: 'Active' as const,
  currentUserRole: 'Owner' as const, createdAt: '2026-08-01T00:00:00Z', updatedAt: '2026-08-01T00:00:00Z'
};
const populated: ProjectOverviewInfo = {
  totalCount: 8, inProgressCount: 2, completedCount: 3, overdueCount: 1, completionRate: 38,
  statusCounts: [
    { status: 'Draft', count: 1 }, { status: 'Approved', count: 1 }, { status: 'InProgress', count: 2 },
    { status: 'QA', count: 1 }, { status: 'Done', count: 3 }
  ],
  recentIssues: [{ id: 'issue-1', title: 'Dashboard cards', status: 'InProgress', priority: 'High', dueDate: null, updatedAt: '2026-08-01T00:00:00Z' }],
  upcomingIssues: [{ id: 'issue-2', title: 'Release check', status: 'QA', priority: 'Critical', dueDate: '2026-08-03T00:00:00Z', updatedAt: '2026-08-01T00:00:00Z' }]
};

afterEach(() => {
  wrappers.splice(0).forEach(wrapper => wrapper.unmount());
  vi.clearAllMocks();
  i18n.global.locale.value = 'en';
});

async function mountView() {
  const router = await createTestRouter('/projects/project-1/overview');
  const wrapper = mount(ProjectOverviewView, {
    global: {
      plugins: [i18n, [PrimeVue, { theme: { preset: Aura } }], router],
      provide: {
        [projectWorkspaceKey as symbol]: {
          project: ref(project), members: ref([{ userId: 'user-1', email: 'owner@example.com', role: 'Owner', createdAt: project.createdAt, updatedAt: project.updatedAt }]),
          currentUser: { id: 'user-1', email: 'owner@example.com', roles: [], permissions: [] },
          reloadProject: vi.fn(), reloadMembers: vi.fn()
        }
      }
    }
  });
  wrappers.push(wrapper);
  await flushPromises();
  return { wrapper, router };
}

describe('ProjectOverviewView', () => {
  it('renders KPI, distribution, issue summaries and navigates to an Issue', async () => {
    vi.mocked(projectsApi.getProjectOverview).mockResolvedValue(populated);
    const { wrapper, router } = await mountView();

    expect(projectsApi.getProjectOverview).toHaveBeenCalledWith('project-1');
    expect(wrapper.text()).toContain('Total Issues');
    expect(wrapper.text()).toContain('38%');
    expect(wrapper.text()).toContain('Dashboard cards');
    expect(wrapper.text()).toContain('Release check');
    expect(wrapper.text()).toContain('Members1');

    const issueLink = wrapper.findAll('a').find(link => link.text().includes('Dashboard cards'))!;
    await issueLink.trigger('click');
    await flushPromises();
    expect(router.currentRoute.value.name).toBe('project-issue-detail');
    expect(router.currentRoute.value.params.issueId).toBe('issue-1');
  });

  it('distinguishes empty data from a failure and supports retry', async () => {
    vi.mocked(projectsApi.getProjectOverview)
      .mockRejectedValueOnce(new Error('Overview failed'))
      .mockResolvedValueOnce({ ...populated, totalCount: 0, inProgressCount: 0, completedCount: 0, overdueCount: 0, completionRate: 0, statusCounts: populated.statusCounts.map(item => ({ ...item, count: 0 })), recentIssues: [], upcomingIssues: [] });
    const { wrapper } = await mountView();

    expect(wrapper.text()).toContain('Overview failed');
    expect(wrapper.text()).not.toContain('Total Issues');
    await wrapper.findAll('button').find(button => button.text() === 'Retry')!.trigger('click');
    await flushPromises();
    expect(projectsApi.getProjectOverview).toHaveBeenCalledTimes(2);
    expect(wrapper.text()).toContain('No Issues yet.');
    expect(wrapper.text()).toContain('No upcoming Issues.');
  });

  it('renders the dashboard in Traditional Chinese', async () => {
    i18n.global.locale.value = 'zh-TW';
    vi.mocked(projectsApi.getProjectOverview).mockResolvedValue(populated);
    const { wrapper } = await mountView();

    expect(wrapper.text()).toContain('議題總數');
    expect(wrapper.text()).toContain('議題狀態分布');
    expect(wrapper.text()).toContain('即將到期');
    expect(wrapper.text()).toContain('進行中');
  });
});
