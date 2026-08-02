import { requestApi } from './http';
import type { SpecAssistantQueryResponse } from './types';

export const querySpecAssistant = (question: string) =>
  requestApi<SpecAssistantQueryResponse>('/api/spec-assistant/query', {
    method: 'POST',
    body: { question }
  });
