import { afterEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import Aura from '@primeuix/themes/aura';
import DashboardView from './DashboardView.vue';
import { i18n } from '../i18n';
import * as dashboardApi from '../api/dashboard';
import { createTestRouter } from '../test/router';

vi.mock('../api/dashboard', () => ({ getDashboard: vi.fn() }));
const wrappers: VueWrapper[]=[];
afterEach(()=>{wrappers.splice(0).forEach(wrapper=>wrapper.unmount());vi.clearAllMocks();});

async function mountView(){const router=await createTestRouter('/dashboard');const wrapper=mount(DashboardView,{props:{refreshKey:0},global:{plugins:[i18n,[PrimeVue,{theme:{preset:Aura}}],router]}});wrappers.push(wrapper);await flushPromises();return{wrapper,router};}

describe('DashboardView',()=>{
  it('renders actionable server-derived work',async()=>{
    vi.mocked(dashboardApi.getDashboard).mockResolvedValue({assignedCount:1,overdueCount:1,statusCounts:[{status:'Draft',count:1}],assignedIssues:[{id:'issue-1',projectId:'project-1',projectName:'PMS',title:'Ship board',status:'Draft',priority:'High',dueDate:null,updatedAt:'2026-08-01T00:00:00Z',labels:[]}],overdueIssues:[],recentActivity:[]});
    const{wrapper,router}=await mountView();expect(wrapper.text()).toContain('Ship board');expect(wrapper.text()).toContain('Assigned to me');await wrapper.findAll('button').find(button=>button.text().includes('Ship board'))!.trigger('click');await flushPromises();expect(router.currentRoute.value.name).toBe('project-issue-detail');
  });
  it('shows an error with retry',async()=>{vi.mocked(dashboardApi.getDashboard).mockRejectedValueOnce(new Error('Dashboard failed')).mockResolvedValueOnce({assignedCount:0,overdueCount:0,statusCounts:[],assignedIssues:[],overdueIssues:[],recentActivity:[]});const{wrapper}=await mountView();expect(wrapper.text()).toContain('Dashboard failed');await wrapper.findAll('button').find(button=>button.text()==='Retry')!.trigger('click');await flushPromises();expect(dashboardApi.getDashboard).toHaveBeenCalledTimes(2);expect(wrapper.text()).toContain('No assigned work');});
});
