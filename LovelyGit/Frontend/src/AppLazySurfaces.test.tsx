// @vitest-environment jsdom
import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import {
	CommitDetailsSurface,
	CommitFileDiffSurface,
	preloadCommitFileDiffSurface,
	WorkingTreeDiffSurface,
} from "./AppLazySurfaces";

vi.mock("./components/CommitDetails/CommitDetails", () => ({
	CommitDetails: () => <div>Loaded commit details</div>,
}));
vi.mock("./components/ConflictResolution/ConflictResolutionView", () => ({
	ConflictResolutionView: () => <div>Loaded conflict resolver</div>,
}));
vi.mock("./components/CommitFileDiff/CommitFileDiffView", () => ({
	CommitFileDiffView: () => <div>Loaded commit diff</div>,
}));

describe("AppLazySurfaces", () => {
	it("renders commit details synchronously without a suspense delay", () => {
		render(
			<CommitDetailsSurface
				commitHash={"a".repeat(40)}
				onOpenFileBlame={vi.fn()}
				onOpenFileHistory={vi.fn()}
				onParentIndexChange={vi.fn()}
				onSelectFile={vi.fn()}
				parentIndex={0}
				repositoryId="repo"
			/>,
		);

		expect(screen.getByText("Loaded commit details")).toBeVisible();
	});

	it("loads the conflict resolver without a suspense throttle", async () => {
		render(
			<WorkingTreeDiffSurface
				file={{
					additions: 1,
					deletions: 1,
					group: "Unmerged",
					isBinary: false,
					oldPath: null,
					path: "conflict.txt",
					status: "Unmerged",
				}}
				onChange={vi.fn()}
				onClose={vi.fn()}
				repositoryId="repo"
			/>,
		);

		expect(await screen.findByText("Loaded conflict resolver")).toBeVisible();
	});

	it("can preload the commit diff surface before a file is selected", async () => {
		await preloadCommitFileDiffSurface();

		render(
			<CommitFileDiffSurface
				commitHash={"a".repeat(40)}
				file={{
					additions: 1,
					deletions: 1,
					isBinary: false,
					path: "file.txt",
					status: "Modified",
				}}
				onClose={vi.fn()}
				parentIndex={0}
				repositoryId="repo"
			/>,
		);

		expect(await screen.findByText("Loaded commit diff")).toBeVisible();
		expect(screen.queryByLabelText("Preparing commit diff")).toBeNull();
	});
});
