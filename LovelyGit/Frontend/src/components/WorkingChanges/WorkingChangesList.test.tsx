// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { WorkingTreeChangedFile } from "@/generated/types";
import { WorkingChangesList, workingFilesOnly } from "./WorkingChangesList";

describe("WorkingChangesList bulk actions", () => {
	it("enables discard for an actionable file delivered in a stale bucket", () => {
		const visibleFile = file("deleted.txt", "Unstaged");
		renderList([visibleFile], workingFilesOnly([visibleFile]));

		expect(
			screen.getByRole("button", { name: "Discard all changes" }),
		).toBeEnabled();
		expect(
			screen.getByRole("button", { name: "Stage all changes" }),
		).toBeEnabled();
	});

	it("does not allow an unresolved conflict to be discarded as a normal edit", () => {
		const conflict = file("conflict.txt", "Unmerged");
		renderList([conflict], workingFilesOnly([conflict]));

		expect(
			screen.getByRole("button", { name: "Discard all changes" }),
		).toBeDisabled();
	});
});

function renderList(
	unstagedFiles: WorkingTreeChangedFile[],
	workingFiles: WorkingTreeChangedFile[],
) {
	render(
		<WorkingChangesList
			isBusy={false}
			isLoading={false}
			onDiscardAll={vi.fn()}
			onDiscardSelected={vi.fn()}
			onIgnorePath={vi.fn()}
			onIndexCommand={vi.fn()}
			onOpenFileBlame={vi.fn()}
			onOpenFileHistory={vi.fn()}
			onSelectFile={vi.fn()}
			onToggleSelected={vi.fn()}
			selectedKeys={new Set()}
			stagedFiles={[]}
			unstagedFiles={unstagedFiles}
			workingFiles={workingFiles}
		/>,
	);
}

function file(
	path: string,
	group: WorkingTreeChangedFile["group"],
): WorkingTreeChangedFile {
	return {
		additions: 0,
		deletions: 1,
		group,
		isBinary: false,
		oldPath: null,
		path,
		status: "Deleted",
	};
}
