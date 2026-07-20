import { requestApi } from './http';
import type { IssueCommentInfo, IssueHistoryInfo, IssueInfo, IssuePriority, IssueStatus, IssueType, PagedResult } from './types';

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

export function createIssue(projectId: string, body: object) {
  return requestApi<IssueInfo>(root(projectId), { method: 'POST', body });
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
