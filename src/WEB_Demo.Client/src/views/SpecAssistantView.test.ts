import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import Aura from '@primeuix/themes/aura';
import SpecAssistantView from './SpecAssistantView.vue';
import { i18n } from '../i18n';
import * as api from '../api/specAssistant';

vi.mock('../api/specAssistant', () => ({ querySpecAssistant: vi.fn() }));
const wrappers: VueWrapper[] = [];

describe('SpecAssistantView', () => {
  beforeEach(() => vi.clearAllMocks());
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
