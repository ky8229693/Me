import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import DashboardView from '../views/DashboardView.vue';
import ProjectsView from '../views/ProjectsView.vue';
import AdminUsersView from '../views/AdminUsersView.vue';
import AdminRolesView from '../views/AdminRolesView.vue';
import ProjectWorkspaceView from '../views/projects/ProjectWorkspaceView.vue';
import ProjectOverviewView from '../views/projects/ProjectOverviewView.vue';
import ProjectMembersView from '../views/projects/ProjectMembersView.vue';
import ProjectSettingsView from '../views/projects/ProjectSettingsView.vue';
import IssueListView from '../views/issues/IssueListView.vue';
import IssueDetailView from '../views/issues/IssueDetailView.vue';
import NotFoundView from '../views/NotFoundView.vue';

declare module 'vue-router' {
  interface RouteMeta {
    permission?: string;
    title?: string;
  }
}

export const routes: RouteRecordRaw[] = [
  { path: '/', redirect: '/dashboard' },
  { path: '/dashboard', name: 'dashboard', component: DashboardView, meta: { title: 'Dashboard' } },
  { path: '/projects', name: 'projects', component: ProjectsView, meta: { title: 'Projects' } },
  { path: '/admin/users', name: 'users', component: AdminUsersView, meta: { title: 'Users', permission: 'users.read' } },
  { path: '/admin/roles', name: 'roles', component: AdminRolesView, meta: { title: 'Roles', permission: 'roles.read' } },
  {
    path: '/projects/:projectId', component: ProjectWorkspaceView,
    children: [
      { path: '', redirect: { name: 'project-overview' } },
      { path: 'overview', name: 'project-overview', component: ProjectOverviewView },
      { path: 'issues', name: 'project-issues', component: IssueListView },
      { path: 'issues/:issueId', name: 'project-issue-detail', component: IssueDetailView },
      { path: 'members', name: 'project-members', component: ProjectMembersView },
      { path: 'settings', name: 'project-settings', component: ProjectSettingsView }
    ]
  },
  { path: '/:pathMatch(.*)*', name: 'not-found', component: NotFoundView, meta: { title: 'Not Found' } }
];

export function createAppRouter(history = createWebHistory()) {
  return createRouter({ history, routes, scrollBehavior: () => ({ top: 0 }) });
}

export const router = createAppRouter();
