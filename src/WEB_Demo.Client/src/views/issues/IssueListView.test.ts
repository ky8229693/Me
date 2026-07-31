import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import { ref } from 'vue';
import PrimeVue from 'primevue/config';
import DataTable from 'primevue/datatable';
import Aura from '@primeuix/themes/aura';
import IssueListView from './IssueListView.vue';
import * as issuesApi from '../../api/issues';
import { projectWorkspaceKey } from '../../features/projects/projectContext';
import { createTestRouter } from '../../test/router';

vi.mock('../../api/issues', async importOriginal => ({
  ...await importOriginal<typeof import('../../api/issues')>(), getIssues: vi.fn()
}));

const project = { id: 'project-1', name: 'PMS', description: null, status: 'Active' as const, currentUserRole: 'Owner' as const, createdAt: '2026-07-20T00:00:00Z', updatedAt: '2026-07-20T00:00:00Z' };
const issue = { id: 'issue-1', projectId: 'project-1', title: 'Search issue', description: null, type: 'Bug' as const, priority: 'High' as const, status: 'Approved' as const, creatorUserId: 'owner', creatorEmail: 'owner@example.com', assigneeUserId: null, assigneeEmail: null, dueDate: null, createdAt: project.createdAt, updatedAt: project.updatedAt, canEdit: true, canAssign: true, canComment: true, allowedTransitions: ['InProgress' as const] };
const wrappers: VueWrapper[] = [];

describe('IssueListView', () => {
  beforeEach(() => {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false, addEventListener: vi.fn(), removeEventListener: vi.fn() }));
    vi.mocked(issuesApi.getIssues).mockResolvedValue({ items: [issue], page: 2, pageSize: 50, totalCount: 70 });
  });
  afterEach(() => { wrappers.splice(0).forEach(wrapper => wrapper.unmount()); vi.clearAllMocks(); vi.unstubAllGlobals(); });

  async function mountView(path = '/projects/project-1/issues?search=Search&type=Bug&page=2&pageSize=50') {
    const router = await createTestRouter(path);
    const wrapper = mount(IssueListView, {
      global: {
        plugins: [[PrimeVue, { theme: { preset: Aura } }], router],
        provide: { [projectWorkspaceKey as symbol]: { project: ref(project), members: ref([]), currentUser: { id: 'owner', email: 'owner@example.com', roles: [], permissions: [] }, reloadProject: vi.fn(), reloadMembers: vi.fn() } },
        stubs: { IssueCreateDialog: true, teleport: true }
      }
    });
    wrappers.push(wrapper); await flushPromises(); return { wrapper, router };
  }

  it('initializes filters and pagination from the route query', async () => {
    await mountView();
    expect(issuesApi.getIssues).toHaveBeenCalledWith('project-1', expect.objectContaining({ search: 'Search', type: 'Bug', page: 2, pageSize: 50 }));
  });

  it('writes applied filters and paginator state to the URL', async () => {
    const { wrapper, router } = await mountView('/projects/project-1/issues');
    await wrapper.get('input[aria-label="Search Issues"]').setValue('database');
    await wrapper.findAll('button').find(button => button.text().includes('Apply'))!.trigger('click');
    await flushPromises();
    expect(router.currentRoute.value.query.search).toBe('database');

    wrapper.getComponent(DataTable).vm.$emit('page', { page: 2, rows: 50 });
    await flushPromises();
    expect(router.currentRoute.value.query).toMatchObject({ search: 'database', page: '3', pageSize: '50' });
  });

  it('opens detail with a return URL that restores the filtered list', async () => {
    const { wrapper, router } = await mountView();
    await wrapper.findAll('button').find(button => button.text().includes('Open'))!.trigger('click');
    await flushPromises();
    expect(router.currentRoute.value.name).toBe('project-issue-detail');
    expect(router.currentRoute.value.params.issueId).toBe('issue-1');
    expect(router.currentRoute.value.query.from).toContain('search=Search');
  });
});
