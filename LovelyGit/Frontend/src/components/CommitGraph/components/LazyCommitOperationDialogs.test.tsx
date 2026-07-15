// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import {
	LazyCherryPickDialog,
	LazyRevertDialog,
} from "./LazyCommitOperationDialogs";

vi.mock("./CherryPickDialog", () => ({
	CherryPickDialog: ({ commits }: { commits: CommitGraphRow[] | null }) =>
		commits?.length ? <div>Cherry-pick dialog loaded</div> : null,
}));

vi.mock("./RevertDialog", () => ({
	RevertDialog: ({ commits }: { commits: CommitGraphRow[] | null }) =>
		commits?.length ? <div>Revert dialog loaded</div> : null,
}));

describe("LazyCommitOperationDialogs", () => {
	it("does not load a closed operation and renders it when activated", async () => {
		const props = {
			commits: null,
			currentBranchName: "main",
			onOpenChange: vi.fn(),
			onOpenWorkingChanges: vi.fn(),
			onRepositoryChanged: vi.fn(),
			repositoryId: "repo",
		};
		const view = render(<LazyRevertDialog {...props} />);
		expect(screen.queryByText("Revert dialog loaded")).not.toBeInTheDocument();
		view.rerender(
			<LazyRevertDialog {...props} commits={[{} as CommitGraphRow]} />,
		);
		expect(await screen.findByText("Revert dialog loaded")).toBeVisible();
	});

	it("reveals cherry-pick after selecting commits", async () => {
		const props = {
			commits: [{} as CommitGraphRow],
			currentBranchName: "main",
			onOpenChange: vi.fn(),
			onOpenWorkingChanges: vi.fn(),
			onRepositoryChanged: vi.fn(),
			repositoryId: "repo",
		};
		render(<LazyCherryPickDialog {...props} />);
		expect(await screen.findByText("Cherry-pick dialog loaded")).toBeVisible();
	});
});
