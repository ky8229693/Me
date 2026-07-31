import { createApp } from 'vue';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import App from './App.vue';
import { router } from './router';
import 'primeicons/primeicons.css';
import './styles/tailwind.css';
import './styles/styles.scss';

createApp(App)
  .use(PrimeVue, {
    theme: {
      preset: Aura,
      options: {
        darkModeSelector: '.app-dark'
      }
    }
  })
  .use(ToastService)
  .use(router)
  .mount('#app');
