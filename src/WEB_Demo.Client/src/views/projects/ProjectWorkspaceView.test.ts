import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import Aura from '@primeuix/themes/aura';
import ProjectWorkspaceView from './ProjectWorkspaceView.vue';
import { i18n } from '../../i18n';
import * as projectsApi from '../../api/projects';
import { createTestRouter } from '../../test/router';

vi.mock('../../api/projects', () => ({ getProject: vi.fn(), getProjectMembers: vi.fn() }));

const project = {
  id: 'project-1', name: 'PMS', description: 'Core workspace', status: 'Active' as const,
  currentUserRole: 'Owner' as const, createdAt: '2026-07-20T00:00:00Z', updatedAt: '2026-07-20T00:00:00Z'
};
const user = { id: 'owner', email: 'owner@example.com', roles: ['User'], permissions: [] };
const wrappers: VueWrapper[] = [];

describe('ProjectWorkspaceView', () => {
  beforeEach(() => {
    vi.mocked(projectsApi.getProject).mockResolvedValue(project);
    vi.mocked(projectsApi.getProjectMembers).mockResolvedValue([]);
  });
  afterEach(() => { wrappers.splice(0).forEach(wrapper => wrapper.unmount()); vi.clearAllMocks(); });

  async function mountWorkspace(path = '/projects/project-1/issues') {
    const router = await createTestRouter(path);
    const wrapper = mount(ProjectWorkspaceView, {
      props: { currentUser: user },
      global: {
        plugins: [i18n, [PrimeVue, { theme: { preset: Aura } }], router],
        stubs: { RouterView: true }
      }
    });
    wrappers.push(wrapper);
    await flushPromises();
    return { wrapper, router };
  }

  it('loads the routed project context and exposes workspace child navigation', async () => {
    const { wrapper, router } = await mountWorkspace();
    expect(projectsApi.getProject).toHaveBeenCalledWith('project-1');
    expect(wrapper.text()).toContain('PMS');
    expect(wrapper.text()).toContain('Overview');
    expect(wrapper.text()).toContain('Issues');
    expect(wrapper.text()).toContain('Members');
    expect(wrapper.text()).toContain('Settings');
    await wrapper.get('a[href="/projects/project-1/members"]').trigger('click');
    await flushPromises();
    expect(router.currentRoute.value.name).toBe('project-members');
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false);
  });

  it('omits Settings for a Member and identifies archived projects', async () => {
    vi.mocked(projectsApi.getProject).mockResolvedValue({ ...project, status: 'Archived', currentUserRole: 'Member' });
    const { wrapper } = await mountWorkspace();
    expect(wrapper.text()).toContain('This project is archived');
    expect(wrapper.text()).not.toContain('Settings');
  });

  it('shows a Not Found style state when membership does not expose the project', async () => {
    vi.mocked(projectsApi.getProject).mockResolvedValue(null);
    const { wrapper } = await mountWorkspace('/projects/hidden-project/overview');
    expect(wrapper.text()).toContain('Project not found or not accessible');
    expect(wrapper.text()).not.toContain('Core workspace');
  });
});
