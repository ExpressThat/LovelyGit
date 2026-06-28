import type {
	AppTheme,
	CommitDiffLineDisplayMode,
	CommitDiffViewMode,
	RemotePrimaryAction,
} from "@/generated/types";

export type Settings = {
	CommitDiffContextLines: number;
	CommitDiffIgnoreWhitespace: boolean;
	CommitDiffLineDisplayMode: CommitDiffLineDisplayMode;
	CommitDiffViewMode: CommitDiffViewMode;
	CommitDiffWrapLines: boolean;
	CurrentGitRepositoryId: string | null;
	RemotePrimaryAction: RemotePrimaryAction;
	Theme: AppTheme;
};
export type SettingsKey = keyof Settings;

export const DEFAULT_SETTINGS: Settings = {
	CommitDiffContextLines: 8,
	CommitDiffIgnoreWhitespace: false,
	CommitDiffLineDisplayMode: "Changes",
	CommitDiffViewMode: "SideBySide",
	CommitDiffWrapLines: false,
	CurrentGitRepositoryId: null,
	RemotePrimaryAction: "Fetch",
	Theme: "System",
};
