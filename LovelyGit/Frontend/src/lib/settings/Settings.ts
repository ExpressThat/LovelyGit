import type {
	AppTheme,
	CommitDiffLineDisplayMode,
	CommitDiffViewMode,
} from "@/generated/types";

export type Settings = {
	CommitDiffContextLines: number;
	CommitDiffLineDisplayMode: CommitDiffLineDisplayMode;
	CommitDiffViewMode: CommitDiffViewMode;
	CommitDiffWrapLines: boolean;
	CurrentGitRepositoryId: string | null;
	Theme: AppTheme;
};
export type SettingsKey = keyof Settings;

export const DEFAULT_SETTINGS: Settings = {
	CommitDiffContextLines: 8,
	CommitDiffLineDisplayMode: "Changes",
	CommitDiffViewMode: "SideBySide",
	CommitDiffWrapLines: false,
	CurrentGitRepositoryId: null,
	Theme: "System",
};
