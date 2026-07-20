export type MenuItem = {
  id: 'dashboard' | 'projects' | 'users' | 'roles';
  label: string;
  icon: string;
  active: boolean;
};
