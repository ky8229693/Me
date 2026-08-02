import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import Aura from '@primeuix/themes/aura';
import ProjectsView from './ProjectsView.vue';
import { i18n } from '../i18n';
import * as projectApi from '../api/projects';

const routerPush = vi.fn();
vi.mock('vue-router', async importOriginal => ({
  ...await importOriginal<typeof import('vue-router')>(),
  useRouter: () => ({ push: routerPush })
}));

const toastAdd = vi.fn();
vi.mock('primevue/usetoast', () => ({ useToast: () => ({ add: toastAdd }) }));
vi.mock('../api/projects', async importOriginal => {
  const original = await importOriginal<typeof import('../api/projects')>();
  return {
    ...original,
    getProjects: vi.fn(), createProject: vi.fn(), updateProject: vi.fn(),
    archiveProject: vi.fn(), restoreProject: vi.fn(), getProjectMembers: vi.fn(),
    searchProjectMemberCandidates: vi.fn(), addProjectMember: vi.fn(),
    updateProjectMemberRole: vi.fn(), removeProjectMember: vi.fn()
  };
});

const activeProject = {
  id: 'project-1', name: 'PMS', description: 'Core', status: 'Active' as const,
  currentUserRole: 'Owner' as const, createdAt: '2026-07-20T00:00:00Z', updatedAt: '2026-07-20T00:00:00Z'
};
const wrappers: VueWrapper[] = [];

describe('ProjectsView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({
      matches: false,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn()
    }));
    vi.mocked(projectApi.getProjects).mockResolvedValue([activeProject]);
    vi.mocked(projectApi.getProjectMembers).mockResolvedValue([]);
    vi.mocked(projectApi.searchProjectMemberCandidates).mockResolvedValue([]);
    vi.mocked(projectApi.createProject).mockResolvedValue({ ...activeProject, id: 'project-2', name: 'New Project' });
  });

  afterEach(() => {
    wrappers.splice(0).forEach(wrapper => wrapper.unmount());
    vi.unstubAllGlobals();
  });

  function mountView(permissions: string[] = ['projects.create']) {
    const wrapper = mount(ProjectsView, {
      props: { currentUser: { id: 'owner', email: 'owner@example.com', roles: ['Admin'], permissions } },
      global: {
        plugins: [i18n, [PrimeVue, { theme: { preset: Aura } }]],
        stubs: { teleport: true }
      }
    });
    wrappers.push(wrapper);
    return wrapper;
  }

  it('lists accessible projects and exposes creation only with permission', async () => {
    const wrapper = mountView();
    await flushPromises();
    expect(wrapper.text()).toContain('PMS');
    expect(wrapper.text()).toContain('Create project');

    const viewer = mountView([]);
    await flushPromises();
    expect(viewer.text()).not.toContain('Create project');
  });

  it('opens a project as a routed workspace instead of a Dialog', async () => {
    const wrapper = mountView();
    await flushPromises();
    const open = wrapper.findAll('button').find(button => button.text().includes('Open'))!;
    await open.trigger('click');
    await flushPromises();

    expect(routerPush).toHaveBeenCalledWith({ name: 'project-overview', params: { projectId: 'project-1' } });
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false);
  });

  it('shows archived status in the list and still routes to the workspace', async () => {
    vi.mocked(projectApi.getProjects).mockResolvedValue([{ ...activeProject, status: 'Archived' }]);
    const wrapper = mountView();
    await flushPromises();
    await wrapper.findAll('button').find(button => button.text().includes('Open'))!.trigger('click');
    await flushPromises();

    expect(wrapper.text()).toContain('Archived');
    expect(routerPush).toHaveBeenCalledWith({ name: 'project-overview', params: { projectId: 'project-1' } });
  });

  it('does not load members while rendering the project list', async () => {
    vi.mocked(projectApi.getProjects).mockResolvedValue([{
      ...activeProject, currentUserRole: 'Member'
    }]);
    vi.mocked(projectApi.getProjectMembers).mockResolvedValue([
      { userId: 'owner', email: 'owner@example.com', role: 'Owner', createdAt: activeProject.createdAt, updatedAt: activeProject.updatedAt },
      { userId: 'manager', email: 'manager@example.com', role: 'Manager', createdAt: activeProject.createdAt, updatedAt: activeProject.updatedAt }
    ]);
    const wrapper = mountView([]);
    await flushPromises();
    expect(projectApi.getProjectMembers).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('Member');
  });
});
