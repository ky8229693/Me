<script setup lang="ts">
import Button from 'primevue/button';
import Checkbox from 'primevue/checkbox';
import Column from 'primevue/column';
import DataTable from 'primevue/datatable';
import Tag from 'primevue/tag';
import type { TodoItem } from '../../api/types';

defineProps<{
  todos: TodoItem[];
}>();

const emit = defineEmits<{
  toggle: [todo: TodoItem];
  delete: [todo: TodoItem];
}>();
</script>

<template>
  <DataTable :value="todos" class="todo-table" show-gridlines>
    <Column style="width: 72px" body-style="text-align: center">
      <template #body="{ data }">
        <Checkbox :model-value="data.isDone" binary @update:model-value="emit('toggle', data)" />
      </template>
    </Column>

    <Column header="Title" field="title">
      <template #body="{ data }">
        <span class="todo-title" :class="{ done: data.isDone }">{{ data.title }}</span>
      </template>
    </Column>

    <Column header="Status" style="width: 120px">
      <template #body="{ data }">
        <Tag
          :icon="data.isDone ? 'pi pi-check-circle' : undefined"
          :severity="data.isDone ? 'success' : 'info'"
          :value="data.isDone ? 'Done' : 'Pending'"
        />
      </template>
    </Column>

    <Column header="Actions" style="width: 100px" body-style="text-align: right">
      <template #body="{ data }">
        <Button icon="pi pi-trash" label="Delete" severity="danger" text @click="emit('delete', data)" />
      </template>
    </Column>
  </DataTable>
</template>
