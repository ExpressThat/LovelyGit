// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { CommitChangedFile, RepositoryStashItem } from "@/generated/types";
import { StashInspectionDialog } from "./StashInspectionDialog";
import { useStashInspection } from "./useStashInspection";

vi.mock("./useStashInspection", () => ({ useStashInspection: vi.fn() }));
vi.mock("./StashInspectionFileList", () => ({
	StashInspectionFileList: ({
		files,
		onSelect,
	}: {
		files: Array<{ file: CommitChangedFile }>;
		onSelect: (file: unknown) => void;
	}) => (
		<button onClick={() => onSelect(files[0])} type="button">
			Select {files[0]?.file.path}
		</button>
	),
}));
vi.mock("../CommitFileDiff/CommitFileDiffView", () => ({
	CommitFileDiffView: ({
		commitHash,
		file,
		onClose,
	}: {
		commitHash: string;
		file: CommitChangedFile;
		onClose: () => void;
	}) => (
		<div>
			<span>
				Diff {commitHash}:{file.path}
			</span>
			<button onClick={onClose} type="button">
				Close diff
			</button>
		</div>
	),
}));

const inspect = vi.mocked(useStashInspection);
const retry = vi.fn();
const stash: RepositoryStashItem = {
	commitHash: "stash-commit",
	createdAtUnixSeconds: 1_700_000_000,
	message: "checkpoint",
	selector: "stash@{0}",
};
const changedFile: CommitChangedFile = {
	additions: 2,
	deletions: 1,
	isBinary: false,
	path: "src/app.ts",
	status: "Modified",
};

describe("StashInspectionDialog", () => {
	beforeEach(() => vi.clearAllMocks());

	it("opens a selected tracked file without a suspense throttle", async () => {
		const user = userEvent.setup();
		inspect.mockReturnValue({
			retry,
			state: {
				files: [
					{ commitHash: "stash-commit", file: changedFile, source: "Tracked" },
				],
				status: "loaded",
				tracked: details(),
				untracked: null,
			},
		});
		render(
			<StashInspectionDialog
				onClose={vi.fn()}
				repositoryId="repo"
				stash={stash}
			/>,
		);

		expect(screen.getByText("Inspect stash@{0}")).toBeVisible();
		expect(screen.getByText("1 files")).toBeVisible();
		await user.click(screen.getByRole("button", { name: "Select src/app.ts" }));
		await waitFor(() =>
			expect(screen.getByText("Diff stash-commit:src/app.ts")).toBeVisible(),
		);
		await user.click(screen.getByRole("button", { name: "Close diff" }));
		await waitFor(() =>
			expect(screen.getByText("Choose a stashed file")).toBeVisible(),
		);
	});

	it("keeps an inspection failure retryable without closing the stash", async () => {
		const user = userEvent.setup();
		inspect.mockReturnValue({
			retry,
			state: { message: "Pack object unavailable", status: "error" },
		});
		render(
			<StashInspectionDialog
				onClose={vi.fn()}
				repositoryId="repo"
				stash={stash}
			/>,
		);

		expect(screen.getByText("Pack object unavailable")).toBeVisible();
		await user.click(screen.getByRole("button", { name: "Retry" }));
		expect(retry).toHaveBeenCalledOnce();
	});
});

function details() {
	return {
		author: "Tester",
		body: "",
		branches: [],
		changedFiles: [changedFile],
		date: 1,
		email: "test@example.com",
		hash: "stash-commit",
		hasLineStats: true,
		message: "stash",
		parents: ["head", "index"],
		signatureKind: "None" as const,
		stats: { additions: 2, deletions: 1 },
		subject: "stash",
		tags: [],
	};
}
