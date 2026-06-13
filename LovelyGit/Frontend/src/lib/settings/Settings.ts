import type { SettingValueMap } from "@/generated/LovelyGit.CommandContracts";

export type Settings = SettingValueMap;
export type SettingsKey = keyof Settings;

export const DEFAULT_SETTINGS: Settings = {
	CommitDiffContextLines: 8,
	CommitDiffLineDisplayMode: "Changes",
	CommitDiffViewMode: "SideBySide",
	CommitDiffWrapLines: false,
	CurrentGitRepositoryId: null,
	Theme: "System",
};
