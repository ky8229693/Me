<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import Message from 'primevue/message';
import Skeleton from 'primevue/skeleton';
import Tag from 'primevue/tag';
import { useToast } from 'primevue/usetoast';
import TodoForm from '../features/todos/TodoForm.vue';
import TodoTable from '../features/todos/TodoTable.vue';
import { createTodo, deleteTodo as deleteTodoItem, getTodos, toggleTodo as toggleTodoItem } from '../api/todos';
import type { TodoItem } from '../api/types';

const props = defineProps<{
  refreshKey: number;
}>();

const toast = useToast();
const todos = ref<TodoItem[]>([]);
const isLoading = ref(false);
const errorMessage = ref('');

const remainingCount = computed(() => todos.value.filter(todo => !todo.isDone).length);
const completedCount = computed(() => todos.value.length - remainingCount.value);

function notify(severity: 'success' | 'info' | 'warn' | 'error', detail: string) {
  toast.add({ severity, detail, life: 2600 });
}

async function loadTodos() {
  isLoading.value = true;
  errorMessage.value = '';

  try {
    todos.value = await getTodos();
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Unknown error.';
  } finally {
    isLoading.value = false;
  }
}

async function addTodo(rawTitle: string) {
  const title = rawTitle.trim();
  if (!title) {
    notify('warn', 'Please enter a todo title.');
    return;
  }

  try {
    const todo = await createTodo(title);

    if (todo) {
      todos.value.push(todo);
    }

    notify('success', 'Created.');
  } catch (error) {
    notify('error', error instanceof Error ? error.message : 'Create failed.');
  }
}

async function toggleTodo(todo: TodoItem) {
  try {
    const updatedTodo = await toggleTodoItem(todo.id);

    if (updatedTodo) {
      todos.value = todos.value.map(item => (item.id === updatedTodo.id ? updatedTodo : item));
    }
  } catch (error) {
    notify('error', error instanceof Error ? error.message : 'Update failed.');
  }
}

async function deleteTodo(todo: TodoItem) {
  try {
    await deleteTodoItem(todo.id);

    todos.value = todos.value.filter(item => item.id !== todo.id);
    notify('success', 'Deleted.');
  } catch (error) {
    notify('error', error instanceof Error ? error.message : 'Delete failed.');
  }
}

watch(
  () => props.refreshKey,
  () => {
    void loadTodos();
  }
);

onMounted(loadTodos);
</script>

<template>
  <section class="workspace border-surface bg-surface-0 dark:bg-surface-900">
    <header class="page-header">
      <div>
        <p class="eyebrow text-primary">ASP.NET Core API + Vue 3 + PrimeVue</p>
        <h1>Dashboard</h1>
      </div>

      <div class="summary">
        <Tag severity="warn" :value="`${remainingCount} pending`" />
        <Tag severity="success" :value="`${completedCount} done`" />
      </div>
    </header>

    <TodoForm @add="addTodo" @refresh="loadTodos" />

    <Message
      v-if="errorMessage"
      class="content-alert"
      severity="error"
      :closable="false"
    >
      {{ errorMessage }}
    </Message>

    <div v-if="isLoading" class="skeleton-list" aria-label="Loading todo items">
      <Skeleton v-for="index in 5" :key="index" height="2.75rem" />
    </div>

    <div v-else-if="todos.length === 0" class="empty-state">
      <i class="pi pi-inbox" aria-hidden="true"></i>
      <span>No todo items yet</span>
    </div>

    <TodoTable v-else :todos="todos" @toggle="toggleTodo" @delete="deleteTodo" />
  </section>
</template>
