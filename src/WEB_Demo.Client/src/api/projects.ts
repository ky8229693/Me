import { requestApi } from './http';
import type { ProjectInfo, ProjectMemberCandidate, ProjectMemberInfo } from './types';

export const projectRoles = ['Owner', 'Manager', 'Member', 'Viewer'] as const;

export function getProjects(): Promise<ProjectInfo[] | null> {
  return requestApi<ProjectInfo[]>('/api/projects');
}

export function createProject(name: string, description: string | null): Promise<ProjectInfo | null> {
  return requestApi<ProjectInfo>('/api/projects', { method: 'POST', body: { name, description } });
}

export function updateProject(projectId: string, name: string, description: string | null): Promise<ProjectInfo | null> {
  return requestApi<ProjectInfo>(`/api/projects/${encodeURIComponent(projectId)}`, {
    method: 'PUT', body: { name, description }
  });
}

export function archiveProject(projectId: string): Promise<ProjectInfo | null> {
  return requestApi<ProjectInfo>(`/api/projects/${encodeURIComponent(projectId)}/archive`, { method: 'POST' });
}

export function restoreProject(projectId: string): Promise<ProjectInfo | null> {
  return requestApi<ProjectInfo>(`/api/projects/${encodeURIComponent(projectId)}/restore`, { method: 'POST' });
}

export function getProjectMembers(projectId: string): Promise<ProjectMemberInfo[] | null> {
  return requestApi<ProjectMemberInfo[]>(`/api/projects/${encodeURIComponent(projectId)}/members`);
}

export function searchProjectMemberCandidates(projectId: string, search: string): Promise<ProjectMemberCandidate[] | null> {
  const query = new URLSearchParams({ search });
  return requestApi<ProjectMemberCandidate[]>(
    `/api/projects/${encodeURIComponent(projectId)}/member-candidates?${query.toString()}`
  );
}

export function addProjectMember(projectId: string, email: string, role: string): Promise<ProjectMemberInfo | null> {
  return requestApi<ProjectMemberInfo>(`/api/projects/${encodeURIComponent(projectId)}/members`, {
    method: 'POST', body: { email, role }
  });
}

export function updateProjectMemberRole(projectId: string, userId: string, role: string): Promise<ProjectMemberInfo | null> {
  return requestApi<ProjectMemberInfo>(
    `/api/projects/${encodeURIComponent(projectId)}/members/${encodeURIComponent(userId)}/role`,
    { method: 'PUT', body: { role } }
  );
}

export function removeProjectMember(projectId: string, userId: string): Promise<ProjectMemberInfo | null> {
  return requestApi<ProjectMemberInfo>(
    `/api/projects/${encodeURIComponent(projectId)}/members/${encodeURIComponent(userId)}`,
    { method: 'DELETE' }
  );
}
