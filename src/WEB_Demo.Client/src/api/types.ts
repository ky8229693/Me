export type ApiResponse<T> = {
  success: boolean;
  message: string | null;
  data: T | null;
  errorCode: string | null;
};

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly errorCode: string | null
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export type AuthUser = {
  id: string;
  email: string;
  roles: string[];
  permissions: string[];
};

export type AuthResponse = {
  accessToken: string;
  expiresAt: string;
  user: AuthUser;
};

export type RegisterRequest = {
  email: string;
  password: string;
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type AdminUser = {
  id: string;
  email: string;
  isEnabled: boolean;
  roles: string[];
};

export type AdminRole = {
  id: string;
  name: string;
  isSystem: boolean;
  userCount: number;
  permissions?: string[];
};

export type PermissionInfo = {
  key: string;
  name: string;
  description: string;
  category: string;
};

export type RolePermissions = {
  roleId: string;
  roleName: string;
  isSystem: boolean;
  permissions: string[];
};

export type ProjectInfo = {
  id: string;
  name: string;
  description: string | null;
  status: 'Active' | 'Archived';
  currentUserRole: 'Owner' | 'Manager' | 'Member' | 'Viewer';
  createdAt: string;
  updatedAt: string;
};

export type ProjectMemberInfo = {
  userId: string;
  email: string;
  role: 'Owner' | 'Manager' | 'Member' | 'Viewer';
  createdAt: string;
  updatedAt: string;
};

export type ProjectMemberCandidate = {
  userId: string;
  email: string;
};

export type ProjectOverviewIssueInfo = {
  id: string;
  title: string;
  status: IssueStatus;
  priority: IssuePriority;
  dueDate: string | null;
  updatedAt: string;
};

export type ProjectOverviewInfo = {
  totalCount: number;
  inProgressCount: number;
  completedCount: number;
  overdueCount: number;
  completionRate: number;
  statusCounts: { status: IssueStatus; count: number }[];
  recentIssues: ProjectOverviewIssueInfo[];
  upcomingIssues: ProjectOverviewIssueInfo[];
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type IssueType = 'Feature' | 'Bug' | 'ChangeRequest';
export type IssuePriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type IssueStatus = 'Draft' | 'Approved' | 'InProgress' | 'QA' | 'Done';

export type IssueLabelInfo = { id: string; name: string; color: string };

export type IssueInfo = {
  id: string;
  projectId: string;
  title: string;
  description: string | null;
  type: IssueType;
  priority: IssuePriority;
  status: IssueStatus;
  creatorUserId: string;
  creatorEmail: string;
  assigneeUserId: string | null;
  assigneeEmail: string | null;
  dueDate: string | null;
  createdAt: string;
  updatedAt: string;
  canEdit: boolean;
  canAssign: boolean;
  canComment: boolean;
  allowedTransitions: IssueStatus[];
  labels: IssueLabelInfo[];
};

export type IssueBoardInfo = { items: IssueInfo[]; total: number; isTruncated: boolean; statuses: IssueStatus[] };

export type DashboardIssueInfo = {
  id: string; projectId: string; projectName: string; title: string; status: IssueStatus;
  priority: IssuePriority; dueDate: string | null; updatedAt: string; labels: IssueLabelInfo[];
};
export type DashboardActivityInfo = { projectId: string; projectName: string; issueId: string | null; description: string; createdAt: string };
export type DashboardInfo = {
  assignedCount: number; overdueCount: number; statusCounts: { status: IssueStatus; count: number }[];
  assignedIssues: DashboardIssueInfo[]; overdueIssues: DashboardIssueInfo[]; recentActivity: DashboardActivityInfo[];
};

export type SpecAssistantQueryResponse = {
  answer: string;
  relatedSpecs: string[];
  sources: string[];
};

export type ProjectDraft = { id: string; name: string; description: string | null; expiresAt: string };

export type SpecIndexStatus = {
  retrievalMode: 'Hybrid' | 'Embedding' | 'Lexical';
  status: 'Unbuilt' | 'Building' | 'Ready' | 'Unavailable' | 'NotApplicable';
  model: string | null;
  chunkCount: number;
  builtAt: string | null;
  store: 'PostgreSql' | 'Memory';
  inserted: number;
  updated: number;
  deleted: number;
  unchanged: number;
};

export type IssueHistoryInfo = {
  id: string;
  actorUserId: string;
  actorEmail: string;
  changeType: string;
  fieldName: string | null;
  previousValue: string | null;
  newValue: string | null;
  createdAt: string;
};

export type IssueCommentInfo = {
  id: string;
  authorUserId: string;
  authorEmail: string;
  content: string;
  createdAt: string;
};

export type AssistantContext = { projectId: string | null; issueId: string | null };
export type AssistantMessage = { id: string; role: 'User' | 'Assistant' | 'Tool'; content: string; createdAt: string };
export type AssistantPendingAction = {
  id: string; type: 'CreateProject' | 'CreateIssue'; status: string;
  preview: Record<string, unknown>; expiresAt: string; result: Record<string, unknown> | null;
};
export type AssistantConversationSummary = {
  id: string; title: string | null; context: AssistantContext; createdAt: string; updatedAt: string;
};
export type AssistantConversation = AssistantConversationSummary & {
  messages: AssistantMessage[]; pendingAction: AssistantPendingAction | null;
};
export type AssistantTurn = {
  message: AssistantMessage; pendingAction: AssistantPendingAction | null; sources: string[]; state: string;
};
export type AssistantConfirmation = {
  action: AssistantPendingAction; message: string; resource: unknown;
};
