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
vi.mock("./components/ui/sonner", () => ({ Toaster: () => null }));

const defaultProps = {
	createBranchOpen: false,
	currentBranchName: "main",
	fileBlameTarget: null,
	fileHistoryTarget: null,
	isCommitSearchOpen: false,
	isCommandPaletteOpen: false,
	remoteManagerOpen: false,
	onBranchChanged: vi.fn(),
	onCommandPaletteOpenChange: vi.fn(),
	onCreateBranchOpenChange: vi.fn(),
	onFileBlameOpenChange: vi.fn(),
	onFileHistoryOpenChange: vi.fn(),
	onSearchOpenChange: vi.fn(),
	onOpenSettings: vi.fn(),
	onRemoteManagerOpenChange: vi.fn(),
	onRepositoryChanged: vi.fn(),
	onOpenWorkingChanges: vi.fn(),
	onRefreshRepository: vi.fn(),
	onSelectCommit: vi.fn(),
	repositoryId: "repo",
};

describe("AppOverlays", () => {
	it("shares branch and remote dialogs with external entry points", () => {
		const view = render(<AppOverlays {...defaultProps} createBranchOpen />);
		expect(screen.getByText("Create branch loaded")).toBeVisible();
		view.rerender(<AppOverlays {...defaultProps} remoteManagerOpen />);
		expect(screen.getByText("Remote manager loaded")).toBeVisible();
	});

	it("loads an overlay only after it is requested and retains it for exit", async () => {
		const view = render(<AppOverlays {...defaultProps} />);
		expect(screen.queryByText(/Search loaded/)).not.toBeInTheDocument();

		view.rerender(<AppOverlays {...defaultProps} isCommitSearchOpen />);
		expect(await screen.findByText("Search loaded: true")).toBeVisible();

		view.rerender(<AppOverlays {...defaultProps} />);
		expect(await screen.findByText("Search loaded: false")).toBeVisible();
	});
});
