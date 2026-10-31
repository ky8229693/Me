import { requestApi } from './http';
import type { ProjectDraft, ProjectInfo, SpecAssistantQueryResponse, SpecIndexStatus } from './types';

export const querySpecAssistant = (question: string) =>
  requestApi<SpecAssistantQueryResponse>('/api/spec-assistant/query', {
    method: 'POST',
    body: { question }
  });

export const getSpecIndexStatus = () =>
  requestApi<SpecIndexStatus>('/api/spec-assistant/index');

export const rebuildSpecIndex = () =>
  requestApi<SpecIndexStatus>('/api/spec-assistant/index/rebuild', { method: 'POST' });

export const createProjectDraft = (prompt: string) =>
  requestApi<ProjectDraft>('/api/spec-assistant/project-drafts', { method: 'POST', body: { prompt } });

export const confirmProjectDraft = (draftId: string) =>
  requestApi<ProjectInfo>(`/api/spec-assistant/project-drafts/${draftId}/confirm`, { method: 'POST' });
