// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type {
	CommitGraphRow,
	InteractiveRebaseCommit,
} from "@/generated/types";
import { useInteractiveRebasePlan } from "../hooks/useInteractiveRebasePlan";
import { InteractiveRebaseDialog } from "./InteractiveRebaseDialog";

vi.mock("../hooks/useInteractiveRebasePlan", () => ({
	useInteractiveRebasePlan: vi.fn(),
}));

describe("InteractiveRebaseDialog", () => {
	it("bounds the maximum rebase plan rendering and preserves row actions", () => {
		const commits = Array.from({ length: 100 }, (_, index) => commit(index));
		const move = vi.fn();
		vi.mocked(useInteractiveRebasePlan).mockReturnValue({
			error: null,
			isLoading: false,
			isRunning: false,
			move,
			plan: commits.map((item) => ({
				action: "Pick",
				hash: item.hash,
				message: item.subject,
			})),
			response: {
				baseCommitHash: "f".repeat(40),
				commits,
				currentBranchName: "main",
			},
			start: vi.fn(),
			updateAction: vi.fn(),
			updateMessage: vi.fn(),
			validationError: null,
		});
		const startedAt = performance.now();
		render(
			<InteractiveRebaseDialog
				baseCommit={baseCommit()}
				currentBranchName="main"
				onOpenChange={vi.fn()}
				onOpenWorkingChanges={vi.fn()}
				onRepositoryChanged={vi.fn()}
				repositoryId="repo"
			/>,
		);
		const elapsed = performance.now() - startedAt;

		expect(
			screen.getAllByRole("combobox", { name: /Action for/ }),
		).toHaveLength(8);
		expect(elapsed).toBeLessThan(220);
		expect(
			screen.queryByRole("combobox", { name: /Rebase commit 99/ }),
		).toBeNull();
		fireEvent.click(
			screen.getByRole("button", { name: "Move Rebase commit 0 down" }),
		);
		expect(move).toHaveBeenCalledWith(0, 1);
	});

	it("surfaces plan loading failures and disables the mutation", () => {
		vi.mocked(useInteractiveRebasePlan).mockReturnValue({
			error: "Could not read the rebase plan.",
			isLoading: false,
			isRunning: false,
			move: vi.fn(),
			plan: [],
			response: null,
			start: vi.fn(),
			updateAction: vi.fn(),
			updateMessage: vi.fn(),
			validationError: null,
		});

		render(
			<InteractiveRebaseDialog
				baseCommit={baseCommit()}
				currentBranchName="main"
				onOpenChange={vi.fn()}
				onOpenWorkingChanges={vi.fn()}
				onRepositoryChanged={vi.fn()}
				repositoryId="repo"
			/>,
		);

		expect(screen.getByText("Could not read the rebase plan.")).toBeVisible();
		expect(
			screen.getByRole("button", { name: "Rebase 0 commits" }),
		).toBeDisabled();
	});

	it("persists reword input through the plan controller", () => {
		const item = commit(0);
		const updateMessage = vi.fn();
		vi.mocked(useInteractiveRebasePlan).mockReturnValue({
			error: null,
			isLoading: false,
			isRunning: false,
			move: vi.fn(),
			plan: [{ action: "Reword", hash: item.hash, message: item.subject }],
			response: {
				baseCommitHash: "f".repeat(40),
				commits: [item],
				currentBranchName: "main",
			},
			start: vi.fn(),
			updateAction: vi.fn(),
			updateMessage,
			validationError: null,
		});

		render(
			<InteractiveRebaseDialog
				baseCommit={baseCommit()}
				currentBranchName="main"
				onOpenChange={vi.fn()}
				onOpenWorkingChanges={vi.fn()}
				onRepositoryChanged={vi.fn()}
				repositoryId="repo"
			/>,
		);

		fireEvent.input(
			screen.getByRole("textbox", { name: "New message for Rebase commit 0" }),
			{ target: { value: "Polished message" } },
		);
		expect(updateMessage).toHaveBeenCalledWith(item.hash, "Polished message");
	});
});

function commit(index: number): InteractiveRebaseCommit {
	return {
		authorName: "LovelyGit Performance",
		authorUnixSeconds: 1_700_000_000 + index,
		hash: index.toString(16).padStart(40, "0"),
		subject: `Rebase commit ${index}`,
	};
}

function baseCommit() {
	return {
		commit: { hash: "f".repeat(40) },
	} as CommitGraphRow;
}
