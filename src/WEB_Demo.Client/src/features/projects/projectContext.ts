import type { InjectionKey, Ref } from 'vue';
import type { AuthUser, ProjectInfo, ProjectMemberInfo } from '../../api/types';

export type ProjectWorkspaceContext = {
  project: Ref<ProjectInfo>;
  members: Ref<ProjectMemberInfo[]>;
  currentUser: AuthUser;
  reloadProject: () => Promise<void>;
  reloadMembers: () => Promise<void>;
};

export const projectWorkspaceKey: InjectionKey<ProjectWorkspaceContext> = Symbol('project-workspace');
