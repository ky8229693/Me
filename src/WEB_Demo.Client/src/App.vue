<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { CircleCheck, Delete, Plus, Refresh } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';

type ApiResponse<T> = {
  success: boolean;
  message: string | null;
  data: T | null;
  errorCode: string | null;
};

type TodoItem = {
  id: number;
  title: string;
  isDone: boolean;
};

const todos = ref<TodoItem[]>([]);
const newTitle = ref('');
const isLoading = ref(false);
const errorMessage = ref('');

const remainingCount = computed(() => todos.value.filter(todo => !todo.isDone).length);
const completedCount = computed(() => todos.value.length - remainingCount.value);

async function readApiResponse<T>(response: Response): Promise<ApiResponse<T>> {
  const body = (await response.json()) as ApiResponse<T>;

  if (!response.ok || !body.success) {
    throw new Error(body.message ?? 'API request failed.');
  }

  return body;
}

async function loadTodos() {
  isLoading.value = true;
  errorMessage.value = '';

  try {
    const response = await fetch('/api/todos');
    const body = await readApiResponse<TodoItem[]>(response);
    todos.value = body.data ?? [];
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
    const response = await fetch('/api/todos', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ title })
    });
    const body = await readApiResponse<TodoItem>(response);

    if (body.data) {
      todos.value.push(body.data);
    }

    newTitle.value = '';
    ElMessage.success(body.message ?? 'Created.');
  } catch (error) {
    ElMessage.error(error instanceof Error ? error.message : 'Create failed.');
  }
}

async function toggleTodo(todo: TodoItem) {
  try {
    const response = await fetch(`/api/todos/${todo.id}/toggle`, {
      method: 'PATCH'
    });
    const body = await readApiResponse<TodoItem>(response);

    if (body.data) {
      todos.value = todos.value.map(item => (item.id === body.data?.id ? body.data : item));
    }
  } catch (error) {
    ElMessage.error(error instanceof Error ? error.message : 'Update failed.');
  }
}

async function deleteTodo(todo: TodoItem) {
  try {
    const response = await fetch(`/api/todos/${todo.id}`, {
      method: 'DELETE'
    });
    const body = await readApiResponse<null>(response);

    todos.value = todos.value.filter(item => item.id !== todo.id);
    ElMessage.success(body.message ?? 'Deleted.');
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
