<script setup lang="ts">
import { ref } from 'vue';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';

const emit = defineEmits<{
  add: [title: string];
  refresh: [];
}>();

const title = ref('');

function submit() {
  emit('add', title.value);
  title.value = '';
}
</script>

<template>
  <form class="todo-form" @submit.prevent="submit">
    <InputText
      v-model="title"
      class="todo-input"
      placeholder="Add a todo item"
      @keyup.enter="submit"
    />
    <Button type="submit" icon="pi pi-plus" label="Add" />
    <Button type="button" icon="pi pi-refresh" label="Refresh" severity="secondary" @click="emit('refresh')" />
  </form>
</template>
