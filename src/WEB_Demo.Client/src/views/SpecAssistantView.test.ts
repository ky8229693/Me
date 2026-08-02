import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import Aura from '@primeuix/themes/aura';
import SpecAssistantView from './SpecAssistantView.vue';
import { i18n } from '../i18n';
import * as api from '../api/specAssistant';

vi.mock('../api/specAssistant', () => ({ querySpecAssistant: vi.fn(), getSpecIndexStatus: vi.fn(), rebuildSpecIndex: vi.fn() }));
const wrappers: VueWrapper[] = [];

describe('SpecAssistantView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.getSpecIndexStatus).mockResolvedValue({ retrievalMode: 'Hybrid', status: 'Ready', model: 'bge-m3', chunkCount: 42, builtAt: '2026-08-02T00:00:00Z', store: 'PostgreSql', inserted: 0, updated: 0, deleted: 0, unchanged: 0 });
  });
  afterEach(() => wrappers.splice(0).forEach(wrapper => wrapper.unmount()));

  function mountView() {
    const wrapper = mount(SpecAssistantView, { global: { plugins: [i18n, [PrimeVue, { theme: { preset: Aura } }]] } });
    wrappers.push(wrapper);
    return wrapper;
  }

  it('validates a blank question without calling the API', async () => {
    const wrapper = mountView();
    await wrapper.get('form').trigger('submit');
    expect(api.querySpecAssistant).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('Enter a question.');
  });

  it('renders a grounded answer, related specs, and sources', async () => {
    vi.mocked(api.querySpecAssistant).mockResolvedValue({ answer: 'Use issue-management.', relatedSpecs: ['issue-management'], sources: ['openspec/specs/issue-management/spec.md'] });
    const wrapper = mountView();
    await wrapper.get('textarea').setValue('Which spec?');
    await wrapper.get('form').trigger('submit');
    await flushPromises();
    expect(wrapper.text()).toContain('Use issue-management.');
    expect(wrapper.text()).toContain('openspec/specs/issue-management/spec.md');
  });

  it('shows persistent Hybrid index status and synchronizes it', async () => {
    vi.mocked(api.rebuildSpecIndex).mockResolvedValue({ retrievalMode: 'Hybrid', status: 'Ready', model: 'bge-m3', chunkCount: 43, builtAt: '2026-08-02T01:00:00Z', store: 'PostgreSql', inserted: 1, updated: 0, deleted: 0, unchanged: 42 });
    const wrapper = mountView();
    await flushPromises();
    expect(wrapper.text()).toContain('bge-m3');
    expect(wrapper.text()).toContain('42');
    await wrapper.findAll('button').find(button => button.text().includes('Sync specifications'))!.trigger('click');
    await flushPromises();
    expect(api.rebuildSpecIndex).toHaveBeenCalledOnce();
    expect(wrapper.text()).toContain('43');
  });

  it('shows a recoverable error when the query fails', async () => {
    vi.mocked(api.querySpecAssistant).mockRejectedValue(new Error('upstream'));
    const wrapper = mountView();
    await wrapper.get('textarea').setValue('Which spec?');
    await wrapper.get('form').trigger('submit');
    await flushPromises();
    expect(wrapper.text()).toContain('Unable to answer.');
    expect(wrapper.text()).toContain('Retry');
  });
});
