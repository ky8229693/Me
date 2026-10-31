import { computed } from 'vue';
import { useI18n } from 'vue-i18n';

export const projectStatuses = ['Active', 'Archived'] as const;
export const issueStatuses = ['Draft', 'Approved', 'InProgress', 'QA', 'Done'] as const;
export const issueTypes = ['Feature', 'Bug', 'ChangeRequest'] as const;
export const issuePriorities = ['Low', 'Medium', 'High', 'Critical'] as const;
export const projectRoles = ['Owner', 'Manager', 'Member', 'Viewer'] as const;

export function useDomainLabels() {
  const { t } = useI18n();
  const label = (group: string, value: string) => {
    const key = `domain.${group}.${value}`;
    const translated = t(key);
    return translated === key ? value : translated;
  };
  const options = (group: string, values: readonly string[]) => computed(() => values.map(value => ({ value, label: label(group, value) })));
  return {
    projectStatusLabel: (value: string) => label('projectStatus', value),
    issueStatusLabel: (value: string) => label('issueStatus', value),
    issueTypeLabel: (value: string) => label('issueType', value),
    priorityLabel: (value: string) => label('priority', value),
    projectRoleLabel: (value: string) => label('projectRole', value),
    roleLabel: (value: string) => label('role', value),
    projectStatusOptions: options('projectStatus', projectStatuses),
    issueStatusOptions: options('issueStatus', issueStatuses),
    issueTypeOptions: options('issueType', issueTypes),
    priorityOptions: options('priority', issuePriorities),
    projectRoleOptions: options('projectRole', projectRoles)
  };
}
