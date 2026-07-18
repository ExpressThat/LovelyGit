// @vitest-environment jsdom

import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { StashInspectionFileList } from "./StashInspectionFileList";
import type { StashInspectionFile } from "./useStashInspection";

vi.mock("@tanstack/react-virtual", () => ({
	useVirtualizer: ({ count }: { count: number }) => ({
		getTotalSize: () => count * 48,
		getVirtualItems: () => [],
	}),
}));

const tracked = inspectionFile("tracked.txt", "Modified", "Tracked");
const untracked = inspectionFile("notes.txt", "Added", "Untracked");

describe("StashInspectionFileList", () => {
	it("keeps tracked and untracked sources distinct and selects the exact file", async () => {
		const user = userEvent.setup();
		const onSelect = vi.fn();
		render(
			<StashInspectionFileList
				files={[tracked, untracked]}
				onSelect={onSelect}
				selected={tracked}
			/>,
		);

		const list = screen.getByRole("region", { name: "Stashed files" });
		expect(list).toHaveAttribute("data-stashed-files-list", "ordinary");
		expect(screen.getByText("Tracked · Modified")).toBeVisible();
		expect(screen.getByText("Untracked · Added")).toBeVisible();
		expect(screen.getByTitle("tracked.txt")).toHaveAttribute(
			"aria-current",
			"true",
		);
		await user.click(screen.getByTitle("notes.txt"));
		expect(onSelect).toHaveBeenCalledWith(untracked);
	});

	it("bounds a large stash while retaining its complete scroll range", () => {
		const files = Array.from({ length: 2_000 }, (_, index) =>
			inspectionFile(
				`group/file-${index.toString().padStart(4, "0")}.txt`,
				"Modified",
				"Tracked",
			),
		);
		render(
			<StashInspectionFileList
				files={files}
				onSelect={vi.fn()}
				selected={null}
			/>,
		);

		const list = screen.getByRole("region", { name: "Stashed files" });
		expect(list).toHaveAttribute("data-stashed-files-list", "virtual");
		expect(within(list).getAllByRole("button")).toHaveLength(10);
		expect(within(list).getByTitle("group/file-0000.txt")).toBeVisible();
		expect(within(list).queryByTitle("group/file-1999.txt")).toBeNull();
		expect(list.firstElementChild).toHaveStyle({ height: "96000px" });
	});
});

function inspectionFile(
	path: string,
	status: string,
	source: StashInspectionFile["source"],
): StashInspectionFile {
	return {
		commitHash: source === "Tracked" ? "stash" : "untracked-parent",
		file: { additions: 1, deletions: 0, isBinary: false, path, status },
		source,
	};
}
