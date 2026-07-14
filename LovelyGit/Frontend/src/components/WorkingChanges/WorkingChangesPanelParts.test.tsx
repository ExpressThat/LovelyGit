// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { WorkingTreeChangedFile } from "@/generated/types";
import {
	fileKey,
	selectedStashPaths,
	WorkingChangesHeader,
} from "./WorkingChangesPanelParts";

describe("WorkingChangesHeader", () => {
	it("disables refresh while a working-tree scan is active", () => {
		render(
			<WorkingChangesHeader isLoading onRefresh={vi.fn()} totalCount={1_800} />,
		);

		expect(
			screen.getByRole("button", { name: "Refresh working changes" }),
		).toBeDisabled();
	});
});

describe("selectedStashPaths", () => {
	it("deduplicates staged and unstaged selections and excludes conflicts", () => {
		const staged = file("shared.txt", "Staged");
		const unstaged = file("shared.txt", "Unstaged");
		const untracked = file("new file.txt", "Untracked");
		const unmerged = file("conflict.txt", "Unmerged");

		expect(
			selectedStashPaths(
				{
					staged: [staged],
					unstaged: [unstaged],
					untracked: [untracked],
					unmerged: [unmerged],
					totalCount: 4,
				},
				new Set([
					fileKey(staged),
					fileKey(unstaged),
					fileKey(untracked),
					fileKey(unmerged),
				]),
			),
		).toEqual(["shared.txt", "new file.txt"]);
	});
});

function file(
	path: string,
	group: WorkingTreeChangedFile["group"],
): WorkingTreeChangedFile {
	return {
		additions: 1,
		deletions: 0,
		group,
		isBinary: false,
		oldPath: null,
		path,
		status: "Modified",
	};
}
