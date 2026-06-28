import type {
	AppTheme,
	CommitDiffLineDisplayMode,
	CommitDiffViewMode,
	ConflictFileViewMode,
	RemotePrimaryAction,
} from "@/generated/types";

export type Settings = {
	CommitDiffContextLines: number;
	CommitDiffIgnoreWhitespace: boolean;
	CommitDiffLineDisplayMode: CommitDiffLineDisplayMode;
	CommitDiffViewMode: CommitDiffViewMode;
	CommitDiffWrapLines: boolean;
	CommitGraphRefsPanelOpen: boolean;
	ConflictFileViewMode: ConflictFileViewMode;
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
	CommitGraphRefsPanelOpen: true,
	ConflictFileViewMode: "Path",
	CurrentGitRepositoryId: null,
	RemotePrimaryAction: "Fetch",
	Theme: "System",
};
