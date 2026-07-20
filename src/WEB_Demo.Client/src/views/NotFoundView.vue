<script setup lang="ts">
import Button from 'primevue/button';
import { useRouter } from 'vue-router';
const router = useRouter();
</script>
<template><section class="workspace state-page"><i class="pi pi-map-marker"></i><h1>Page not found</h1><p>The requested page does not exist or is no longer available.</p><Button label="Back to dashboard" @click="router.push({ name: 'dashboard' })" /></section></template>
<style scoped>.state-page { min-height: 24rem; display: grid; place-content: center; justify-items: center; gap: .8rem; text-align: center; }.state-page i { font-size: 2rem; color: var(--p-primary-color); }.state-page h1,.state-page p { margin: 0; }</style>
