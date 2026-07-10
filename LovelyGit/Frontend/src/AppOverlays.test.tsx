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
vi.mock("./components/ui/sonner", () => ({ Toaster: () => null }));

const defaultProps = {
	fileBlameTarget: null,
	fileHistoryTarget: null,
	isCommitSearchOpen: false,
	onFileBlameOpenChange: vi.fn(),
	onFileHistoryOpenChange: vi.fn(),
	onSearchOpenChange: vi.fn(),
	onSelectCommit: vi.fn(),
	repositoryId: "repo",
};

describe("AppOverlays", () => {
	it("loads an overlay only after it is requested and retains it for exit", async () => {
		const view = render(<AppOverlays {...defaultProps} />);
		expect(screen.queryByText(/Search loaded/)).not.toBeInTheDocument();

		view.rerender(<AppOverlays {...defaultProps} isCommitSearchOpen />);
		expect(await screen.findByText("Search loaded: true")).toBeVisible();

		view.rerender(<AppOverlays {...defaultProps} />);
		expect(await screen.findByText("Search loaded: false")).toBeVisible();
	});
});
