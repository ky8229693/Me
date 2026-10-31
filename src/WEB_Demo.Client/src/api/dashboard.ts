import { requestApi } from './http';
import type { DashboardInfo } from './types';

export const getDashboard = () => requestApi<DashboardInfo>('/api/dashboard');
