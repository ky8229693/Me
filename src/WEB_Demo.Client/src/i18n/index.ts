import { createI18n } from 'vue-i18n';
import { ref } from 'vue';
import { en, zhTW } from './messages';
import { englishPrimeVueLocale, primeVueLocales } from './primeVueLocales';

export const supportedLocales = ['zh-TW', 'en'] as const;
export type AppLocale = (typeof supportedLocales)[number];
export const localeStorageKey = 'web_demo.locale';

export function isAppLocale(value: unknown): value is AppLocale {
  return typeof value === 'string' && supportedLocales.includes(value as AppLocale);
}

export function readInitialLocale(storage: Pick<Storage, 'getItem'> | undefined = globalThis.localStorage): AppLocale {
  const saved = storage?.getItem(localeStorageKey);
  return isAppLocale(saved) ? saved : 'zh-TW';
}

const initialLocale: AppLocale = import.meta.env.MODE === 'test' ? 'en' : readInitialLocale();
export const currentLocale = ref<AppLocale>(initialLocale);
export const i18n = createI18n({ legacy: false, locale: currentLocale.value, fallbackLocale: 'zh-TW', messages: { 'zh-TW': zhTW, en } });

export function primeVueLocaleFor(locale: AppLocale) {
  return locale === 'zh-TW' ? primeVueLocales['zh-TW'] : englishPrimeVueLocale;
}

export function setLocale(next: AppLocale, primeVueConfig?: { locale?: object }) {
  currentLocale.value = next;
  i18n.global.locale.value = next;
  globalThis.localStorage?.setItem(localeStorageKey, next);
  if (globalThis.document) document.documentElement.lang = next;
  if (primeVueConfig) primeVueConfig.locale = { ...primeVueLocaleFor(next) };
}

if (globalThis.document) document.documentElement.lang = currentLocale.value;
