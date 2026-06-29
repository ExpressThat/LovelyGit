import { describe, expect, it, vi } from "vitest";
import type { WorkingTreeChangedFile } from "@/generated/types";
import {
	buildWorkingChangesGroups,
	buildWorkingChangesVirtualRows,
} from "./WorkingChangesGroupRows";
import { getWorkingChangesVirtualListHeight } from "./WorkingChangesGroups";

describe("buildWorkingChangesVirtualRows", () => {
	it("builds headers only for non-empty groups", () => {
		const groups = buildWorkingChangesGroups({
			isBusy: false,
			onDiscardSelected: vi.fn(),
			onIndexCommand: vi.fn(),
			onToggleSelected: vi.fn(),
			selectedKeys: new Set(),
			stagedFiles: [],
			unmergedFiles: [file("conflict.txt", "Unmerged")],
			workingFiles: [file("changed.txt", "Unstaged")],
		});

		const rows = buildWorkingChangesVirtualRows(groups);

		expect(rows.map((row) => row.id)).toEqual([
			"Changes:header",
			"Unstaged:Modified:changed.txt",
			"Unmerged:header",
			"Unmerged:Modified:conflict.txt",
		]);
	});

	it("caps the visible list height for large change sets", () => {
		expect(getWorkingChangesVirtualListHeight(2)).toBe(108);
		expect(getWorkingChangesVirtualListHeight(300)).toBe(488);
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
