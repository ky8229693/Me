export type MessageTree = { [key: string]: string | MessageTree };

export const zhTW: MessageTree = {
  common: {
    appName: 'WEB Demo', language: '介面語言', traditionalChinese: '繁體中文', english: 'English',
    refresh: '重新整理', retry: '重試', create: '建立', save: '儲存', cancel: '取消', delete: '刪除', edit: '編輯',
    add: '新增', remove: '移除', open: '開啟', actions: '操作', status: '狀態', name: '名稱', description: '說明',
    loading: '載入中…', noData: '目前沒有資料', unassigned: '未指派', active: '啟用', disabled: '停用', archived: '已封存',
    search: '搜尋', apply: '套用', close: '關閉', confirm: '確認', email: '電子郵件', password: '密碼', role: '角色',
    due: '到期日 {date}', error: '操作失敗，請稍後再試。'
  },
  layout: {
    toggleNavigation: '切換導覽列', closeNavigation: '關閉導覽列', mainNavigation: '主要導覽', refreshDashboard: '重新整理工作台', logout: '登出',
    workbench: '工作台', dashboard: '工作台', specAssistant: '規格助理', projectManagement: '專案管理', projects: '專案', systemAdministration: '系統管理', users: '使用者管理', roles: '角色管理'
  },
  auth: {
    restoring: '正在恢復登入狀態…', authentication: '身分驗證', login: '登入', register: '註冊', signIn: '登入', createAccount: '建立帳號',
    loginTitle: '登入', registerTitle: '建立帳號', loginSubtitle: '歡迎回來，請輸入帳號密碼以繼續。', registerSubtitle: '建立帳號以開始管理您的工作空間。',
    mode: '驗證模式', remember: '記住我', forgot: '忘記密碼？', required: '電子郵件與密碼為必填。', passwordLength: '密碼至少需要 6 個字元。',
    failed: '驗證失敗。', signedIn: '登入成功。', accountCreated: '帳號已建立。', signedOut: '已登出。', sessionExpired: '登入狀態已過期，請重新登入。'
  },
  dashboard: {
    eyebrow: '我的工作台', title: '工作台', subtitle: '聚焦於所有進行中專案裡指派給您及已逾期的工作。', loading: '正在載入工作台', loadFailed: '無法載入工作台。',
    assigned: '指派給我', overdue: '已逾期', assignedWork: '指派工作', noAssigned: '目前沒有指派工作。', nothingOverdue: '目前沒有逾期工作。', recentActivity: '最近活動', noActivity: '目前沒有活動紀錄。'
  },
  specAssistant: {
    eyebrow: 'AI 練習・第三階段', title: '規格助理', subtitle: '使用 PostgreSQL pgvector 持久化檢索 OpenSpec 內容，再交給 LLM 回答。',
    question: '想詢問哪項規格？', placeholder: '例如：誰可以建立或指派議題？', ask: '詢問規格', asking: '正在查詢規格…',
    answer: '回答', relatedSpecs: '相關規格', sources: '來源', noSources: '目前規格沒有可引用的來源。',
    required: '請輸入問題。', failed: '無法取得規格回答，請檢查模型設定後再試。', phaseNote: '第三階段使用 pgvector 持久化向量，並只同步變更的規格。',
    retrieval: '檢索模式', indexStatus: '索引狀態', embeddingModel: 'Embedding 模型', chunkCount: '段落數', builtAt: '同步時間', rebuildIndex: '同步規格', indexLoadFailed: '無法讀取索引狀態。', indexRebuildFailed: '無法同步規格，請檢查 Embedding 與 pgvector 設定。',
    prepareProject: '產生專案草稿', projectDraft: '專案建立預覽', draftExpires: '草稿將於 {time} 過期', confirmCreate: '確認建立專案', draftFailed: '無法從這段文字產生有效的專案草稿。', confirmFailed: '無法建立專案，草稿可能已過期。', projectCreated: '專案「{name}」已建立。'
  },
  projects: {
    eyebrow: '專案管理', title: '專案', subtitle: '選擇專案以開啟工作空間。', create: '建立專案', loadFailed: '無法載入專案。', createFailed: '無法建立專案。',
    empty: '目前沒有可存取的專案。', yourRole: '您的角色', openWorkspace: '開啟工作空間', workspace: '專案工作空間', noDescription: '沒有專案說明。',
    loading: '正在載入專案…', inaccessible: '找不到專案，或您沒有存取權限。', archivedReadOnly: '此專案已封存，所有專案工作皆為唯讀。',
    overview: '總覽', issues: '議題', members: '成員', settings: '設定', overviewMembers: '成員數', quickIssues: '追蹤功能、錯誤與變更', quickMembers: '檢視專案存取與角色',
    overviewSubtitle: '掌握專案進度、工作量與即將到期的議題。', overviewLoading: '正在載入專案總覽…', overviewLoadFailed: '無法載入專案總覽。',
    totalIssues: '議題總數', inProgressIssues: '進行中', completedIssues: '已完成', overdueIssues: '已逾期', completion: '完成進度', completedOfTotal: '已完成 {completed} / {total}',
    projectDetails: '專案資訊', statusDistribution: '議題狀態分布', quickActions: '快速操作', recentIssues: '最近更新', upcomingIssues: '即將到期',
    noRecentIssues: '目前沒有議題。', noUpcomingIssues: '目前沒有即將到期的議題。', updatedOn: '更新於 {date}',
    memberAccess: '專案成員資格控制資料存取權限。', addMember: '新增成員', user: '使用者', projectRole: '專案角色', saveRole: '儲存角色', noMembers: '目前沒有專案成員。',
    settingsTitle: '專案設定', saveChanges: '儲存變更', archive: '封存專案', restore: '恢復專案', restoredOnlyOwner: '只有 Owner 可以恢復已封存專案。', updated: '專案已更新。', updateFailed: '無法更新專案。'
  },
  issues: {
    title: '議題', subtitle: '追蹤功能、錯誤與變更需求。', list: '清單', board: '看板', create: '建立議題', labels: '標籤', searchPlaceholder: '搜尋標題或說明',
    type: '類型', status: '狀態', priority: '優先級', assignee: '負責人', apply: '套用', empty: '沒有符合條件的議題。', loadFailed: '無法載入議題。',
    showing: '目前顯示 {shown} / {total} 個議題，請縮小篩選範圍以查看完整看板。', noIssues: '沒有議題', moveTo: '移動到…', due: '到期日 {date}',
    manageLabels: '管理標籤', newLabel: '新標籤名稱', labelColor: '標籤顏色', addLabel: '新增', noLabels: '目前沒有標籤。', deleteLabel: '刪除標籤',
    detail: '議題詳情', back: '返回議題', created: '議題已建立。', updated: '議題已更新。', transitionFailed: '無法變更議題狀態。',
    titleField: '標題', descriptionField: '說明', dueDate: '到期日', createFailed: '無法建立議題。', save: '儲存議題', comments: '留言', addComment: '新增留言', commentPlaceholder: '輸入留言', history: '變更歷程', details: '詳細資料', metadata: '中繼資料', reporter: '建立者', createdAt: '建立時間', updatedAt: '更新時間', noComments: '目前沒有留言。', noHistory: '目前沒有變更紀錄。', archivedReadOnly: '已封存專案中的議題為唯讀。', loading: '正在載入議題…', unavailable: '無法開啟議題。', moveToStatus: '移動到「{status}」', comment: '留言'
  },
  admin: {
    usersTitle: '使用者管理', usersSubtitle: '管理帳號狀態與角色。', rolesTitle: '角色管理', rolesSubtitle: '管理角色與權限。',
    enabled: '啟用', assignedRoles: '指派角色', permissions: '權限', category: '分類', key: 'Key', createRole: '建立角色', renameRole: '重新命名角色', deleteRole: '刪除角色',
    noUsers: '目前沒有使用者。', noRoles: '目前沒有角色。', loadUsersFailed: '無法載入使用者。', loadRolesFailed: '無法載入角色。', administration: '系統管理', searchEmail: '依電子郵件搜尋', rolesLoadFailed: '無法載入角色選項，因此無法變更角色。', saveRoles: '儲存角色', enable: '啟用', disable: '停用', accountEnabled: '帳號已啟用。', accountDisabled: '帳號已停用。', rolesUpdated: '角色已更新。', type: '類型', system: '系統', custom: '自訂', users: '使用者', rename: '重新命名', roleName: '角色名稱', newRoleName: '新角色名稱', roleCreated: '角色已建立。', roleRenamed: '角色已重新命名。', roleDeleted: '角色已刪除。', savePermissions: '儲存權限', permissionsUpdated: '權限已更新，受影響的登入狀態已撤銷。', adminAllPermissions: 'Admin 是緊急系統角色，永遠擁有所有權限。', roleSubtitle: '角色透過明確權限授予 API 存取；系統 Admin 永遠擁有完整權限。'
  },
  state: { forbiddenTitle: '存取被拒', forbiddenText: '您沒有權限開啟此頁面。', notFoundTitle: '找不到頁面', notFoundText: '要求的頁面不存在或已被移除。', backDashboard: '返回工作台' },
  domain: {
    projectStatus: { Active: '進行中', Archived: '已封存' },
    issueStatus: { Draft: '草稿', Approved: '已核准', InProgress: '進行中', QA: '品質驗證', Done: '已完成' },
    issueType: { Feature: '功能', Bug: '錯誤', ChangeRequest: '變更需求' },
    priority: { Low: '低', Medium: '中', High: '高', Critical: '重大' },
    projectRole: { Owner: '擁有者', Manager: '管理者', Member: '成員', Viewer: '檢視者' },
    role: { Admin: '系統管理員', User: '一般使用者' }
  },
  permission: {
    users_read: { name: '檢視使用者', description: '檢視使用者目錄與帳號資料。' }, users_manage: { name: '管理使用者', description: '啟停帳號並指派角色。' }, roles_read: { name: '檢視角色', description: '檢視角色與權限摘要。' }, roles_manage: { name: '管理角色', description: '建立、重新命名與刪除自訂角色。' }, permissions_manage: { name: '管理權限', description: '檢視權限目錄並更新角色權限。' }, projects_create: { name: '建立專案', description: '建立新的專案工作空間。' }
  }
};

export const en: MessageTree = {
  common: {
    appName: 'WEB Demo', language: 'Interface language', traditionalChinese: '繁體中文', english: 'English', refresh: 'Refresh', retry: 'Retry', create: 'Create', save: 'Save', cancel: 'Cancel', delete: 'Delete', edit: 'Edit', add: 'Add', remove: 'Remove', open: 'Open', actions: 'Actions', status: 'Status', name: 'Name', description: 'Description', loading: 'Loading…', noData: 'No data', unassigned: 'Unassigned', active: 'Active', disabled: 'Disabled', archived: 'Archived', search: 'Search', apply: 'Apply', close: 'Close', confirm: 'Confirm', email: 'Email', password: 'Password', role: 'Role', due: 'Due {date}', error: 'The operation failed. Please try again.'
  },
  layout: { toggleNavigation: 'Toggle navigation', closeNavigation: 'Close navigation', mainNavigation: 'Main navigation', refreshDashboard: 'Refresh dashboard', logout: 'Logout', workbench: 'Workbench', dashboard: 'Dashboard', specAssistant: 'Spec Assistant', projectManagement: 'Project Management', projects: 'Projects', systemAdministration: 'System Administration', users: 'User Management', roles: 'Role Management' },
  auth: { restoring: 'Restoring session…', authentication: 'Authentication', login: 'Login', register: 'Register', signIn: 'Sign In', createAccount: 'Create Account', loginTitle: 'Sign in', registerTitle: 'Create account', loginSubtitle: 'Welcome back. Enter your credentials to continue.', registerSubtitle: 'Create an account to start managing your workspace.', mode: 'Authentication mode', remember: 'Remember me', forgot: 'Forgot password?', required: 'Email and password are required.', passwordLength: 'Password must be at least 6 characters.', failed: 'Authentication failed.', signedIn: 'Signed in.', accountCreated: 'Account created.', signedOut: 'Signed out.', sessionExpired: 'Your session has expired. Please sign in again.' },
  dashboard: { eyebrow: 'My workbench', title: 'Dashboard', subtitle: 'Focus on assigned and overdue work across your active projects.', loading: 'Loading Dashboard', loadFailed: 'Unable to load Dashboard.', assigned: 'Assigned to me', overdue: 'Overdue', assignedWork: 'Assigned work', noAssigned: 'No assigned work.', nothingOverdue: 'Nothing overdue.', recentActivity: 'Recent activity', noActivity: 'No recent activity.' },
  specAssistant: { eyebrow: 'AI practice · Controlled actions', title: 'Specification Assistant', subtitle: 'Ask about OpenSpec or turn a conversation into a safe project draft.', question: 'What would you like the assistant to do?', placeholder: 'For example: Create a website redesign project for the customer portal.', ask: 'Ask specifications', asking: 'Working…', answer: 'Answer', relatedSpecs: 'Related specifications', sources: 'Sources', noSources: 'The current specifications have no source to cite.', required: 'Enter a question.', failed: 'Unable to answer. Check the model configuration and try again.', phaseNote: 'Project actions are previewed and require explicit confirmation before data is created.', retrieval: 'Retrieval mode', indexStatus: 'Index status', embeddingModel: 'Embedding model', chunkCount: 'Chunks', builtAt: 'Synced at', rebuildIndex: 'Sync specifications', indexLoadFailed: 'Unable to load index status.', indexRebuildFailed: 'Unable to synchronize specifications. Check the Embedding and pgvector configuration.', prepareProject: 'Prepare project draft', projectDraft: 'Project creation preview', draftExpires: 'Draft expires at {time}', confirmCreate: 'Confirm project creation', draftFailed: 'Unable to produce a valid project draft from this message.', confirmFailed: 'Unable to create the project. The draft may have expired.', projectCreated: 'Project “{name}” was created.' },
  projects: { eyebrow: 'Project Management', title: 'Projects', subtitle: 'Choose a project to open its workspace.', create: 'Create project', loadFailed: 'Unable to load projects.', createFailed: 'Unable to create project.', empty: 'No accessible projects.', yourRole: 'Your role', openWorkspace: 'Open workspace', workspace: 'Project Workspace', noDescription: 'No project description.', loading: 'Loading project…', inaccessible: 'Project not found or not accessible.', archivedReadOnly: 'This project is archived and all project work is read-only.', overview: 'Overview', issues: 'Issues', members: 'Members', settings: 'Settings', overviewMembers: 'Members', quickIssues: 'Track features, bugs and changes', quickMembers: 'Review project access and roles', overviewSubtitle: 'Track project progress, workload, and upcoming Issues.', overviewLoading: 'Loading project overview…', overviewLoadFailed: 'Unable to load project overview.', totalIssues: 'Total Issues', inProgressIssues: 'In Progress', completedIssues: 'Completed', overdueIssues: 'Overdue', completion: 'Completion', completedOfTotal: '{completed} of {total} completed', projectDetails: 'Project details', statusDistribution: 'Issue status distribution', quickActions: 'Quick actions', recentIssues: 'Recently updated', upcomingIssues: 'Upcoming due dates', noRecentIssues: 'No Issues yet.', noUpcomingIssues: 'No upcoming Issues.', updatedOn: 'Updated {date}', memberAccess: 'Project membership controls access to project data.', addMember: 'Add member', user: 'User', projectRole: 'Project role', saveRole: 'Save role', noMembers: 'No project members.', settingsTitle: 'Project settings', saveChanges: 'Save changes', archive: 'Archive project', restore: 'Restore project', restoredOnlyOwner: 'Only an Owner can restore this archived project.', updated: 'Project updated.', updateFailed: 'Unable to update project.' },
  issues: { title: 'Issues', subtitle: 'Track features, bugs and change requests.', list: 'List', board: 'Board', create: 'Create Issue', labels: 'Labels', searchPlaceholder: 'Search title or description', type: 'Type', status: 'Status', priority: 'Priority', assignee: 'Assignee', apply: 'Apply', empty: 'No matching Issues.', loadFailed: 'Unable to load Issues.', showing: 'Showing {shown} of {total} Issues. Refine the filters to see a complete board.', noIssues: 'No Issues', moveTo: 'Move to…', due: 'Due {date}', manageLabels: 'Manage labels', newLabel: 'New label name', labelColor: 'Label color', addLabel: 'Add', noLabels: 'No labels yet.', deleteLabel: 'Delete label', detail: 'Issue detail', back: 'Back to Issues', created: 'Issue created.', updated: 'Issue updated.', transitionFailed: 'Unable to transition Issue.', titleField: 'Title', descriptionField: 'Description', dueDate: 'Due date', createFailed: 'Unable to create Issue.', save: 'Save Issue', comments: 'Comments', addComment: 'Add comment', commentPlaceholder: 'Write a comment', history: 'History', details: 'Details', metadata: 'Metadata', reporter: 'Reporter', createdAt: 'Created', updatedAt: 'Updated', noComments: 'No comments.', noHistory: 'No history.', archivedReadOnly: 'Archived project Issues are read-only.', loading: 'Loading Issue…', unavailable: 'Issue unavailable.', moveToStatus: 'Move to {status}', comment: 'Comment' },
  admin: { usersTitle: 'User Management', usersSubtitle: 'Manage account status and roles.', rolesTitle: 'Role Management', rolesSubtitle: 'Manage roles and permissions.', enabled: 'Enabled', assignedRoles: 'Assigned roles', permissions: 'Permissions', category: 'Category', key: 'Key', createRole: 'Create role', renameRole: 'Rename role', deleteRole: 'Delete role', noUsers: 'No users.', noRoles: 'No roles.', loadUsersFailed: 'Unable to load users.', loadRolesFailed: 'Unable to load roles.', administration: 'Administration', searchEmail: 'Search by email', rolesLoadFailed: 'Role options could not be loaded. Role changes are disabled.', saveRoles: 'Save roles', enable: 'Enable', disable: 'Disable', accountEnabled: 'Account enabled.', accountDisabled: 'Account disabled.', rolesUpdated: 'Roles updated.', type: 'Type', system: 'System', custom: 'Custom', users: 'Users', rename: 'Rename', roleName: 'Role name', newRoleName: 'New role name', roleCreated: 'Role created.', roleRenamed: 'Role renamed.', roleDeleted: 'Role deleted.', savePermissions: 'Save permissions', permissionsUpdated: 'Permissions updated. Affected sessions were revoked.', adminAllPermissions: 'Admin is the break-glass system role and always has every permission.', roleSubtitle: 'Roles grant API access through explicit permissions. System Admin always has the complete catalog.' },
  state: { forbiddenTitle: 'Access denied', forbiddenText: 'You do not have permission to open this page.', notFoundTitle: 'Page not found', notFoundText: 'The requested page does not exist or is no longer available.', backDashboard: 'Back to dashboard' },
  domain: { projectStatus: { Active: 'Active', Archived: 'Archived' }, issueStatus: { Draft: 'Draft', Approved: 'Approved', InProgress: 'In Progress', QA: 'QA', Done: 'Done' }, issueType: { Feature: 'Feature', Bug: 'Bug', ChangeRequest: 'Change Request' }, priority: { Low: 'Low', Medium: 'Medium', High: 'High', Critical: 'Critical' }, projectRole: { Owner: 'Owner', Manager: 'Manager', Member: 'Member', Viewer: 'Viewer' }, role: { Admin: 'Admin', User: 'User' } },
  permission: { users_read: { name: 'Read users', description: 'Read the user directory and account details.' }, users_manage: { name: 'Manage users', description: 'Enable accounts and assign roles.' }, roles_read: { name: 'Read roles', description: 'Read roles and permission summaries.' }, roles_manage: { name: 'Manage roles', description: 'Create, rename, and delete custom roles.' }, permissions_manage: { name: 'Manage permissions', description: 'Read the permission catalog and update role permissions.' }, projects_create: { name: 'Create projects', description: 'Create new project workspaces.' } }
};
