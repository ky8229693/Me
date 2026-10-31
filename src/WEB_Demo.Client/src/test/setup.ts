import { beforeEach } from 'vitest';
import { config } from '@vue/test-utils';
import { i18n, setLocale } from '../i18n';

config.global.plugins = [i18n];

beforeEach(() => {
  setLocale('en');
});
