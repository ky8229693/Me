import { requestApi } from './http';
import type { SpecAssistantQueryResponse, SpecIndexStatus } from './types';

export const querySpecAssistant = (question: string) =>
  requestApi<SpecAssistantQueryResponse>('/api/spec-assistant/query', {
    method: 'POST',
    body: { question }
  });

export const getSpecIndexStatus = () =>
  requestApi<SpecIndexStatus>('/api/spec-assistant/index');

export const rebuildSpecIndex = () =>
  requestApi<SpecIndexStatus>('/api/spec-assistant/index/rebuild', { method: 'POST' });
