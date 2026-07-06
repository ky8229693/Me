<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { CircleCheck, Delete, Plus, Refresh } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import { createTodo, deleteTodo as deleteTodoItem, getTodos, toggleTodo as toggleTodoItem } from './api/todos';
import type { TodoItem } from './api/types';

const todos = ref<TodoItem[]>([]);
const newTitle = ref('');
const isLoading = ref(false);
const errorMessage = ref('');

const remainingCount = computed(() => todos.value.filter(todo => !todo.isDone).length);
const completedCount = computed(() => todos.value.length - remainingCount.value);

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

async function addTodo() {
  const title = newTitle.value.trim();
  if (!title) {
    ElMessage.warning('Please enter a todo title.');
    return;
  }

  try {
    const todo = await createTodo(title);

    if (todo) {
      todos.value.push(todo);
    }

    newTitle.value = '';
    ElMessage.success('Created.');
  } catch (error) {
    ElMessage.error(error instanceof Error ? error.message : 'Create failed.');
  }
}

async function toggleTodo(todo: TodoItem) {
  try {
    const updatedTodo = await toggleTodoItem(todo.id);

    if (updatedTodo) {
      todos.value = todos.value.map(item => (item.id === updatedTodo.id ? updatedTodo : item));
    }
  } catch (error) {
    ElMessage.error(error instanceof Error ? error.message : 'Update failed.');
  }
}

async function deleteTodo(todo: TodoItem) {
  try {
    await deleteTodoItem(todo.id);

    todos.value = todos.value.filter(item => item.id !== todo.id);
    ElMessage.success('Deleted.');
  } catch (error) {
    ElMessage.error(error instanceof Error ? error.message : 'Delete failed.');
  }
}

onMounted(loadTodos);
</script>

<template>
  <main class="app-shell">
    <section class="workspace">
      <header class="page-header">
        <div>
          <p class="eyebrow">ASP.NET Core API + Vue 3 + Element Plus</p>
          <h1>WEB Demo</h1>
        </div>

        <div class="summary">
          <el-tag type="warning" effect="light">{{ remainingCount }} pending</el-tag>
          <el-tag type="success" effect="light">{{ completedCount }} done</el-tag>
        </div>
      </header>

      <el-form class="todo-form" @submit.prevent="addTodo">
        <el-input
          v-model="newTitle"
          clearable
          size="large"
          placeholder="Add a todo item"
          @keyup.enter="addTodo"
        />
        <el-button native-type="submit" size="large" type="primary" :icon="Plus">Add</el-button>
        <el-button size="large" :icon="Refresh" @click="loadTodos">Refresh</el-button>
      </el-form>

      <el-alert
        v-if="errorMessage"
        class="content-alert"
        type="error"
        :title="errorMessage"
        show-icon
        :closable="false"
      />

      <el-skeleton v-if="isLoading" :rows="5" animated />

      <el-empty v-else-if="todos.length === 0" description="No todo items yet" />

      <el-table v-else :data="todos" class="todo-table" border>
        <el-table-column width="72" align="center">
          <template #default="{ row }">
            <el-checkbox :model-value="row.isDone" @change="toggleTodo(row)" />
          </template>
        </el-table-column>

        <el-table-column label="Title" min-width="220">
          <template #default="{ row }">
            <span class="todo-title" :class="{ done: row.isDone }">{{ row.title }}</span>
          </template>
        </el-table-column>

        <el-table-column label="Status" width="120">
          <template #default="{ row }">
            <el-tag :type="row.isDone ? 'success' : 'info'" effect="plain">
              <el-icon v-if="row.isDone"><CircleCheck /></el-icon>
              {{ row.isDone ? 'Done' : 'Pending' }}
            </el-tag>
          </template>
        </el-table-column>

        <el-table-column label="Actions" width="100" align="right">
          <template #default="{ row }">
            <el-button type="danger" link :icon="Delete" @click="deleteTodo(row)">Delete</el-button>
          </template>
        </el-table-column>
      </el-table>
    </section>
  </main>
</template>
