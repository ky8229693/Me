import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  addProjectMember, archiveProject, createProject, getProjectMembers, getProjects,
  removeProjectMember, restoreProject, searchProjectMemberCandidates, updateProject, updateProjectMemberRole
} from './projects';
import { requestApi } from './http';

vi.mock('./http', () => ({ requestApi: vi.fn() }));
const requestMock = vi.mocked(requestApi);

describe('projects API', () => {
  beforeEach(() => vi.clearAllMocks());

  it('uses project and nested membership routes', async () => {
    await getProjects();
    await createProject('PMS', 'Core');
    await updateProject('project/1', 'PMS 2', null);
    await archiveProject('project/1');
    await restoreProject('project/1');
    await getProjectMembers('project/1');
    await searchProjectMemberCandidates('project/1', 'te st');
    await addProjectMember('project/1', 'user/1', 'Member');
    await updateProjectMemberRole('project/1', 'user/1', 'Viewer');
    await removeProjectMember('project/1', 'user/1');

    expect(requestMock).toHaveBeenNthCalledWith(1, '/api/projects');
    expect(requestMock).toHaveBeenNthCalledWith(2, '/api/projects', { method: 'POST', body: { name: 'PMS', description: 'Core' } });
    expect(requestMock).toHaveBeenNthCalledWith(3, '/api/projects/project%2F1', { method: 'PUT', body: { name: 'PMS 2', description: null } });
    expect(requestMock).toHaveBeenNthCalledWith(4, '/api/projects/project%2F1/archive', { method: 'POST' });
    expect(requestMock).toHaveBeenNthCalledWith(5, '/api/projects/project%2F1/restore', { method: 'POST' });
    expect(requestMock).toHaveBeenNthCalledWith(6, '/api/projects/project%2F1/members');
    expect(requestMock).toHaveBeenNthCalledWith(7, '/api/projects/project%2F1/member-candidates?search=te+st');
    expect(requestMock).toHaveBeenNthCalledWith(8, '/api/projects/project%2F1/members', { method: 'POST', body: { email: 'user/1', role: 'Member' } });
    expect(requestMock).toHaveBeenNthCalledWith(9, '/api/projects/project%2F1/members/user%2F1/role', { method: 'PUT', body: { role: 'Viewer' } });
    expect(requestMock).toHaveBeenNthCalledWith(10, '/api/projects/project%2F1/members/user%2F1', { method: 'DELETE' });
  });
});
