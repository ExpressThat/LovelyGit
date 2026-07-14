// @vitest-environment jsdom
import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { AppOverlays } from "./AppOverlays";

vi.mock("./components/CommitSearch/CommitSearchDialog", () => ({
	CommitSearchDialog: ({ open }: { open: boolean }) => (
		<div>Search loaded: {String(open)}</div>
	),
}));
vi.mock("./components/FileHistory/FileHistoryDialog", () => ({
	FileHistoryDialog: () => <div>History loaded</div>,
}));
vi.mock("./components/FileBlame/FileBlameDialog", () => ({
	FileBlameDialog: () => <div>Blame loaded</div>,
}));
vi.mock("./components/TopNavBar/components/CreateBranchDialog", () => ({
	CreateBranchDialog: ({ open }: { open: boolean }) =>
		open ? <div>Create branch loaded</div> : null,
}));
vi.mock("./components/TopNavBar/components/RemoteManagerDialog", () => ({
	RemoteManagerDialog: ({ open }: { open: boolean }) =>
		open ? <div>Remote manager loaded</div> : null,
}));
vi.mock("./components/WorkingChanges/StashDialog", () => ({
	StashDialog: ({ open }: { open?: boolean }) =>
		open ? <div>Stash manager loaded</div> : null,
}));
vi.mock("./components/Settings/SettingsDialog", () => ({
	SettingsDialog: ({ open }: { open?: boolean }) =>
		open ? <div>Settings loaded</div> : null,
}));
vi.mock("./components/CommandPalette/CommandPalette", () => ({
	CommandPalette: ({ open }: { open: boolean }) =>
		open ? <div>Command palette loaded</div> : null,
}));
vi.mock("./components/ui/sonner", () => ({ Toaster: () => null }));

const defaultProps = {
	canCreateStash: false,
	createBranchOpen: false,
	currentBranchName: "main",
	fileBlameTarget: null,
	fileHistoryTarget: null,
	isCommitSearchOpen: false,
	isCommandPaletteOpen: false,
	remoteManagerOpen: false,
	settingsOpen: false,
	stashOpen: false,
	onBranchChanged: vi.fn(),
	onCommandPaletteOpenChange: vi.fn(),
	onCreateBranchOpenChange: vi.fn(),
	onFileBlameOpenChange: vi.fn(),
	onFileHistoryOpenChange: vi.fn(),
	onSearchOpenChange: vi.fn(),
	onSettingsOpenChange: vi.fn(),
	onOpenSettings: vi.fn(),
	onRemoteManagerOpenChange: vi.fn(),
	onRepositoryChanged: vi.fn(),
	onStashOpenChange: vi.fn(),
	onOpenWorkingChanges: vi.fn(),
	onRefreshRepository: vi.fn(),
	onSelectCommit: vi.fn(),
	repositoryId: "repo",
};

describe("AppOverlays", () => {
	it("shares repository dialogs with external entry points", async () => {
		const view = render(<AppOverlays {...defaultProps} createBranchOpen />);
		expect(await screen.findByText("Create branch loaded")).toBeVisible();
		view.rerender(<AppOverlays {...defaultProps} remoteManagerOpen />);
		expect(await screen.findByText("Remote manager loaded")).toBeVisible();
		view.rerender(<AppOverlays {...defaultProps} stashOpen />);
		expect(await screen.findByText("Stash manager loaded")).toBeVisible();
		view.rerender(<AppOverlays {...defaultProps} settingsOpen />);
		expect(await screen.findByText("Settings loaded")).toBeVisible();
	});

	it("loads an overlay only after it is requested and retains it for exit", async () => {
		const view = render(<AppOverlays {...defaultProps} />);
		expect(screen.queryByText(/Search loaded/)).not.toBeInTheDocument();

		view.rerender(<AppOverlays {...defaultProps} isCommitSearchOpen />);
		expect(await screen.findByText("Search loaded: true")).toBeVisible();

		view.rerender(<AppOverlays {...defaultProps} />);
		expect(await screen.findByText("Search loaded: false")).toBeVisible();
	});

	it("loads the primary command palette only when requested", async () => {
		const view = render(<AppOverlays {...defaultProps} />);
		expect(
			screen.queryByText("Command palette loaded"),
		).not.toBeInTheDocument();

		view.rerender(<AppOverlays {...defaultProps} isCommandPaletteOpen />);
		expect(await screen.findByText("Command palette loaded")).toBeVisible();
	});
});
