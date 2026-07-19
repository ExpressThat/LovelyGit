// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import {
	LazyCheckoutTagDialog,
	LazyCommitComparisonDialog,
	LazyCreateWorktreeDialog,
	LazyDeleteRemoteTagDialog,
	LazyDeleteTagDialog,
	LazyRenameBranchDialog,
} from "./LazyGraphManagementDialogs";

vi.mock("./DeleteTagDialog", () => ({
	DeleteTagDialog: ({ tagName }: { tagName: string | null }) =>
		tagName ? <div>Delete tag dialog loaded</div> : null,
}));
vi.mock("./DeleteRemoteTagDialog", () => ({
	DeleteRemoteTagDialog: ({ tagName }: { tagName: string | null }) =>
		tagName ? <div>Delete remote tag dialog loaded</div> : null,
}));
vi.mock("./CheckoutTagDialog", () => ({
	CheckoutTagDialog: ({ tagName }: { tagName: string }) => (
		<div>Checkout {tagName} loaded</div>
	),
}));
vi.mock("./RenameBranchDialog", () => ({
	RenameBranchDialog: ({ branchName }: { branchName: string }) => (
		<div>Rename {branchName} loaded</div>
	),
}));
vi.mock("./CreateWorktreeDialog", () => ({
	CreateWorktreeDialog: ({ branchName }: { branchName: string }) => (
		<div>Create worktree for {branchName} loaded</div>
	),
}));
vi.mock("./DeferredGraphManagementDialogs", async (importOriginal) => ({
	...(await importOriginal()),
	DeferredCommitComparisonDialog: ({
		base,
		target,
	}: {
		base: CommitGraphRow;
		target: CommitGraphRow;
	}) => (
		<div>
			Deferred {base.commit.hash} with {target.commit.hash}
		</div>
	),
}));

describe("LazyGraphManagementDialogs", () => {
	it("loads a management dialog only after its target becomes active", async () => {
		const props = {
			isBusy: false,
			onConfirm: vi.fn(),
			onOpenChange: vi.fn(),
			tagName: null,
		};
		const view = render(<LazyDeleteTagDialog {...props} />);
		expect(
			screen.queryByText("Delete tag dialog loaded"),
		).not.toBeInTheDocument();
		view.rerender(<LazyDeleteTagDialog {...props} tagName="v1.0.0" />);
		expect(await screen.findByText("Delete tag dialog loaded")).toBeVisible();
	});

	it("reveals remote-tag deletion through the immediate deferred overlay", async () => {
		render(
			<LazyDeleteRemoteTagDialog
				isBusy={false}
				onConfirm={vi.fn()}
				onOpenChange={vi.fn()}
				remoteName="origin"
				tagName="v1.0.0"
			/>,
		);
		expect(
			await screen.findByText("Delete remote tag dialog loaded"),
		).toBeVisible();
	});

	it("reveals the rename dialog after selecting a branch", async () => {
		const props = {
			branchName: "topic",
			existingBranchNames: ["main", "topic"],
			isBusy: false,
			onConfirm: vi.fn(),
			onOpenChange: vi.fn(),
		};
		render(<LazyRenameBranchDialog {...props} />);
		expect(await screen.findByText("Rename topic loaded")).toBeVisible();
	});

	it("reveals checkout after selecting a tag", async () => {
		render(
			<LazyCheckoutTagDialog
				onClose={vi.fn()}
				onRepositoryChanged={vi.fn()}
				repositoryId="repo"
				tagName="v1.0.0"
			/>,
		);
		expect(await screen.findByText("Checkout v1.0.0 loaded")).toBeVisible();
	});

	it("reveals create worktree after selecting a branch", async () => {
		const props = {
			branches: ["main", "topic"],
			existingWorktree: null,
			isBusy: false,
			onBranchChange: vi.fn(),
			onChooseDestination: vi.fn(),
			onClose: vi.fn(),
			onCreate: vi.fn(),
			onOpenExisting: vi.fn(),
		};
		render(<LazyCreateWorktreeDialog {...props} branchName="topic" />);
		expect(
			await screen.findByText("Create worktree for topic loaded"),
		).toBeVisible();
	});

	it("opens commit comparison through the immediate deferred overlay", () => {
		render(
			<LazyCommitComparisonDialog
				base={row("a")}
				onClose={vi.fn()}
				repositoryId="repo"
				target={row("b")}
			/>,
		);

		expect(
			screen.getByText(`Deferred ${"a".repeat(40)} with ${"b".repeat(40)}`),
		).toBeVisible();
	});
});

function row(digit: string) {
	return { commit: { hash: digit.repeat(40) } } as CommitGraphRow;
}
