import type { SettingValueMap } from "@/generated/LovelyGit.CommandContracts";

export type Settings = SettingValueMap;
export type SettingsKey = keyof Settings;

export const DEFAULT_SETTINGS: Settings = {
	CurrentGitRepositoryId: null,
	Theme: "System",
};
