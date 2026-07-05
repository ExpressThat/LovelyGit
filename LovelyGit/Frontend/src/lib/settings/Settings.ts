import type {
	AppFont,
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
	CommitGraphRefsPanelOpen: boolean;
	CurrentGitRepositoryId: string | null;
	Font: AppFont;
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
	CurrentGitRepositoryId: null,
	Font: "Inter",
	RemotePrimaryAction: "Fetch",
	Theme: "System",
};
