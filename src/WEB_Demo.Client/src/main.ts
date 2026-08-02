import { createApp } from 'vue';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import App from './App.vue';
import { router } from './router';
import 'primeicons/primeicons.css';
import './styles/tailwind.css';
import './styles/styles.scss';
import { currentLocale, i18n, primeVueLocaleFor } from './i18n';

createApp(App)
  .use(PrimeVue, {
    locale: primeVueLocaleFor(currentLocale.value),
    theme: {
      preset: Aura,
      options: {
        darkModeSelector: '.app-dark'
      }
    }
  })
  .use(i18n)
  .use(ToastService)
  .use(router)
  .mount('#app');
