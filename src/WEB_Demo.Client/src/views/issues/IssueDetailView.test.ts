import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import { ref } from 'vue';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import IssueDetailView from './IssueDetailView.vue';
import { i18n } from '../../i18n';
import * as issuesApi from '../../api/issues';
import { projectWorkspaceKey } from '../../features/projects/projectContext';
import { createTestRouter } from '../../test/router';

vi.mock('../../api/issues', async importOriginal => ({
  ...await importOriginal<typeof import('../../api/issues')>(),
  getIssue: vi.fn(), getIssueHistory: vi.fn(), getIssueComments: vi.fn(), updateIssue: vi.fn(),
  assignIssue: vi.fn(), transitionIssue: vi.fn(), addIssueComment: vi.fn(), getProjectLabels: vi.fn()
}));

const now = '2026-07-20T00:00:00Z';
const project = { id: 'project-1', name: 'PMS', description: null, status: 'Active' as const, currentUserRole: 'Owner' as const, createdAt: now, updatedAt: now };
const issue = { id: 'issue-1', projectId: 'project-1', title: 'Broken checkout', description: 'Details', type: 'Bug' as const, priority: 'High' as const, status: 'Approved' as const, creatorUserId: 'owner', creatorEmail: 'owner@example.com', assigneeUserId: null, assigneeEmail: null, dueDate: null, createdAt: now, updatedAt: now, canEdit: true, canAssign: true, canComment: true, allowedTransitions: ['InProgress' as const], labels: [] };
const wrappers: VueWrapper[] = [];

describe('IssueDetailView', () => {
  beforeEach(() => {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false, addEventListener: vi.fn(), removeEventListener: vi.fn() }));
    vi.mocked(issuesApi.getIssue).mockResolvedValue(issue);
    vi.mocked(issuesApi.getIssueHistory).mockResolvedValue([{ id: 'history-1', actorUserId: 'owner', actorEmail: 'owner@example.com', changeType: 'Created', fieldName: null, previousValue: null, newValue: 'Approved', createdAt: now }]);
    vi.mocked(issuesApi.getIssueComments).mockResolvedValue([{ id: 'comment-1', authorUserId: 'owner', authorEmail: 'owner@example.com', content: 'Investigating now', createdAt: now }]);
    vi.mocked(issuesApi.getProjectLabels).mockResolvedValue([]);
    vi.mocked(issuesApi.updateIssue).mockResolvedValue(issue);
    vi.mocked(issuesApi.addIssueComment).mockResolvedValue({ id: 'comment-2', authorUserId: 'owner', authorEmail: 'owner@example.com', content: 'Fixed', createdAt: now });
  });
  afterEach(() => { wrappers.splice(0).forEach(wrapper => wrapper.unmount()); vi.clearAllMocks(); vi.unstubAllGlobals(); });

  async function mountView(path = '/projects/project-1/issues/issue-1') {
    const router = await createTestRouter(path);
    const wrapper = mount(IssueDetailView, {
      global: {
        plugins: [i18n, [PrimeVue, { theme: { preset: Aura } }], ToastService, router],
        provide: { [projectWorkspaceKey as symbol]: { project: ref(project), members: ref([]), currentUser: { id: 'owner', email: 'owner@example.com', roles: [], permissions: [] }, reloadProject: vi.fn(), reloadMembers: vi.fn() } },
        stubs: { teleport: true }
      }
    });
    wrappers.push(wrapper); await flushPromises(); return { wrapper, router };
  }

  it('loads a deep-linked Issue with inline comments and history', async () => {
    const { wrapper } = await mountView();
    expect(issuesApi.getIssue).toHaveBeenCalledWith('project-1', 'issue-1');
    expect(wrapper.text()).toContain('Broken checkout');
    expect(wrapper.text()).toContain('Investigating now');
    expect(wrapper.text()).toContain('Created');
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false);
  });

  it('supports detail edits and inline comments', async () => {
    const { wrapper } = await mountView();
    await wrapper.findAll('button').find(button => button.text().includes('Save Issue'))!.trigger('click');
    await wrapper.get('textarea[aria-label="Write a comment"]').setValue('Fixed');
    await wrapper.findAll('button').find(button => button.text() === 'Comment')!.trigger('click');
    await flushPromises();
    expect(issuesApi.updateIssue).toHaveBeenCalledWith('project-1', 'issue-1', expect.objectContaining({ title: 'Broken checkout' }));
    expect(issuesApi.addIssueComment).toHaveBeenCalledWith('project-1', 'issue-1', 'Fixed');
    expect(wrapper.text()).toContain('Fixed');
  });

  it('returns to the preserved list query', async () => {
    const from = '/projects/project-1/issues?search=checkout&page=2';
    const { wrapper, router } = await mountView(`/projects/project-1/issues/issue-1?from=${encodeURIComponent(from)}`);
    await wrapper.findAll('button').find(button => button.text().includes('Back to Issues'))!.trigger('click');
    await flushPromises();
    expect(router.currentRoute.value.fullPath).toBe(from);
  });

  it('shows Not Found without leaking Issue content', async () => {
    vi.mocked(issuesApi.getIssue).mockResolvedValue(null);
    const { wrapper } = await mountView('/projects/project-1/issues/hidden');
    expect(wrapper.text()).toContain('Issue unavailable');
    expect(wrapper.text()).toContain('Issue unavailable');
    expect(wrapper.text()).not.toContain('Broken checkout');
  });
});
