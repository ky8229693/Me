import { defineComponent, h } from 'vue';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';
import { i18n, setLocale } from '.';
import { useDomainLabels } from './domain';

describe('domain localization', () => {
  it('translates labels without changing machine values', () => {
    setLocale('zh-TW');
    const component = defineComponent({
      setup() {
        const labels = useDomainLabels();
        return () => h('div', [
          h('span', { class: 'label' }, labels.issueStatusLabel('InProgress')),
          h('span', { class: 'value' }, labels.issueStatusOptions.value.find(item => item.value === 'InProgress')?.value)
        ]);
      }
    });
    const wrapper = mount(component, { global: { plugins: [i18n] } });
    expect(wrapper.get('.label').text()).toBe('進行中');
    expect(wrapper.get('.value').text()).toBe('InProgress');
  });
});
