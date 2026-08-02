import { requestApi } from './http';
import type { IssueBoardInfo, IssueCommentInfo, IssueHistoryInfo, IssueInfo, IssueLabelInfo, IssuePriority, IssueStatus, IssueType, PagedResult } from './types';

export const issueTypes: IssueType[] = ['Feature', 'Bug', 'ChangeRequest'];
export const issuePriorities: IssuePriority[] = ['Low', 'Medium', 'High', 'Critical'];
export const issueStatuses: IssueStatus[] = ['Draft', 'Approved', 'InProgress', 'QA', 'Done'];

const root = (projectId: string) => `/api/projects/${encodeURIComponent(projectId)}/issues`;

export function getIssues(projectId: string, filters: Record<string, string | number | boolean | null | undefined>) {
  const query = new URLSearchParams();
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== null && value !== undefined && value !== '') query.set(key, String(value));
  });
  return requestApi<PagedResult<IssueInfo>>(`${root(projectId)}?${query}`);
}

export function getIssueBoard(projectId: string, filters: Record<string, string | number | boolean | null | undefined>) {
  const query = new URLSearchParams();
  Object.entries(filters).forEach(([key, value]) => { if (value !== null && value !== undefined && value !== '') query.set(key, String(value)); });
  return requestApi<IssueBoardInfo>(`${root(projectId)}/board?${query}`);
}

const labelsRoot = (projectId: string) => `/api/projects/${encodeURIComponent(projectId)}/labels`;
export const getProjectLabels = (projectId: string) => requestApi<IssueLabelInfo[]>(labelsRoot(projectId));
export const createProjectLabel = (projectId: string, name: string, color: string) =>
  requestApi<IssueLabelInfo>(labelsRoot(projectId), { method: 'POST', body: { name, color } });
export const updateProjectLabel = (projectId: string, labelId: string, name: string, color: string) =>
  requestApi<IssueLabelInfo>(`${labelsRoot(projectId)}/${labelId}`, { method: 'PUT', body: { name, color } });
export const deleteProjectLabel = (projectId: string, labelId: string) =>
  requestApi<object>(`${labelsRoot(projectId)}/${labelId}`, { method: 'DELETE' });

export function createIssue(projectId: string, body: object) {
  return requestApi<IssueInfo>(root(projectId), { method: 'POST', body });
}

export function getIssue(projectId: string, issueId: string) {
  return requestApi<IssueInfo>(`${root(projectId)}/${encodeURIComponent(issueId)}`);
}

export function updateIssue(projectId: string, issueId: string, body: object) {
  return requestApi<IssueInfo>(`${root(projectId)}/${encodeURIComponent(issueId)}`, { method: 'PUT', body });
}

export function assignIssue(projectId: string, issueId: string, assigneeUserId: string | null) {
  return requestApi<IssueInfo>(`${root(projectId)}/${encodeURIComponent(issueId)}/assignee`, {
    method: 'PUT', body: { assigneeUserId }
  });
}

export function transitionIssue(projectId: string, issueId: string, status: string) {
  return requestApi<IssueInfo>(`${root(projectId)}/${encodeURIComponent(issueId)}/transitions`, {
    method: 'POST', body: { status }
  });
}

export function getIssueHistory(projectId: string, issueId: string) {
  return requestApi<IssueHistoryInfo[]>(`${root(projectId)}/${encodeURIComponent(issueId)}/history`);
}

export function getIssueComments(projectId: string, issueId: string) {
  return requestApi<IssueCommentInfo[]>(`${root(projectId)}/${encodeURIComponent(issueId)}/comments`);
}

export function addIssueComment(projectId: string, issueId: string, content: string) {
  return requestApi<IssueCommentInfo>(`${root(projectId)}/${encodeURIComponent(issueId)}/comments`, {
    method: 'POST', body: { content }
  });
}
