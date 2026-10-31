export type MenuItem = {
  routeName: 'dashboard' | 'spec-assistant' | 'projects' | 'users' | 'roles';
  label: string;
  icon: string;
};

export type MenuGroup = { label: string; items: MenuItem[] };
