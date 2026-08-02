import { describe, expect, it } from 'vitest';
import { en, zhTW, type MessageTree } from './messages';
import { isAppLocale, localeStorageKey, readInitialLocale, setLocale } from '.';

function keys(tree: MessageTree, prefix = ''): string[] {
  return Object.entries(tree).flatMap(([key, value]) => {
    const path = prefix ? `${prefix}.${key}` : key;
    return typeof value === 'string' ? [path] : keys(value, path);
  }).sort();
}

describe('localization foundation', () => {
  it('defaults invalid or missing stored locales to Traditional Chinese', () => {
    expect(readInitialLocale({ getItem: () => null })).toBe('zh-TW');
    expect(readInitialLocale({ getItem: () => 'fr' })).toBe('zh-TW');
    expect(isAppLocale('zh-TW')).toBe(true);
    expect(isAppLocale('en')).toBe(true);
  });

  it('keeps the English and Traditional Chinese resource keys in parity', () => {
    expect(keys(en)).toEqual(keys(zhTW));
  });

  it('persists locale and synchronizes html and PrimeVue configuration', () => {
    const config = { locale: {} };
    setLocale('zh-TW', config);
    expect(localStorage.getItem(localeStorageKey)).toBe('zh-TW');
    expect(document.documentElement.lang).toBe('zh-TW');
    expect(config.locale).toMatchObject({ emptyMessage: '目前沒有資料' });
    setLocale('en', config);
    expect(config.locale).toEqual({});
  });
});
