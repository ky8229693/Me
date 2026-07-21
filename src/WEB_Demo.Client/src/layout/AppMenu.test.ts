import { afterEach, describe, expect, it } from 'vitest';
import { mount, type VueWrapper } from '@vue/test-utils';
import AppMenu from './AppMenu.vue';
import { createTestRouter } from '../test/router';

const wrappers: VueWrapper[] = [];

afterEach(() => wrappers.splice(0).forEach(wrapper => wrapper.unmount()));

describe('AppMenu', () => {
  it('renders domain groups, omits empty groups, and marks project child routes active', async () => {
    const router = await createTestRouter('/projects/project-1/issues');
    const wrapper = mount(AppMenu, {
      props: {
        groups: [
          { label: 'Workbench', items: [{ routeName: 'dashboard', label: 'Dashboard', icon: 'pi pi-home' }] },
          { label: 'Project Management', items: [{ routeName: 'projects', label: 'Projects', icon: 'pi pi-briefcase' }] },
          { label: 'System Administration', items: [] }
        ]
      },
      global: { plugins: [router] }
    });
    wrappers.push(wrapper);

    expect(wrapper.text()).toContain('Workbench');
    expect(wrapper.text()).toContain('Project Management');
    expect(wrapper.text()).not.toContain('System Administration');
    expect(wrapper.get('a[href="/projects"]').classes()).toContain('active');
  });
});
