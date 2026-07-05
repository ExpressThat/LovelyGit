import type {
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
	CodeFont: string;
	CurrentGitRepositoryId: string | null;
	DarkAccent: string;
	DarkBackground: string;
	DarkCodeFont: string;
	DarkForeground: string;
	DarkTheme: string;
	DarkUiFont: string;
	Font: string;
	LightAccent: string;
	LightBackground: string;
	LightCodeFont: string;
	LightForeground: string;
	LightTheme: string;
	LightUiFont: string;
	RemotePrimaryAction: RemotePrimaryAction;
	Theme: string;
	UiFont: string;
};
export type SettingsKey = keyof Settings;

export const DEFAULT_SETTINGS: Settings = {
	CommitDiffContextLines: 8,
	CommitDiffIgnoreWhitespace: false,
	CommitDiffLineDisplayMode: "Changes",
	CommitDiffViewMode: "SideBySide",
	CommitDiffWrapLines: false,
	CommitGraphRefsPanelOpen: true,
	CodeFont: "Consolas",
	CurrentGitRepositoryId: null,
	DarkAccent: "",
	DarkBackground: "",
	DarkCodeFont: "Consolas",
	DarkForeground: "",
	DarkTheme: "Midnight",
	DarkUiFont: "Inter",
	Font: "Inter",
	LightAccent: "",
	LightBackground: "",
	LightCodeFont: "Consolas",
	LightForeground: "",
	LightTheme: "Morning",
	LightUiFont: "Inter",
	RemotePrimaryAction: "Fetch",
	Theme: "System",
	UiFont: "Inter",
};
