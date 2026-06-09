import type { SettingValueMap } from "@/generated/LovelyGit.CommandContracts";

export type Settings = SettingValueMap;
export type SettingsKey = keyof Settings;

export const DEFAULT_SETTINGS: Settings = {
	AiComputeDevice: "Gpu",
	AiContextSize: 8192,
	AiFeaturesEnabled: false,
	AiGemmaRawDiffContextPercent: 30,
	AiLlamaRawDiffContextPercent: 50,
	AiModel: "Llama32_3B",
	AiSummaryContextPercent: 20,
	CommitDiffContextLines: 8,
	CommitDiffLineDisplayMode: "Changes",
	CommitDiffViewMode: "SideBySide",
	CommitDiffWrapLines: false,
	CurrentGitRepositoryId: null,
	Theme: "System",
};
