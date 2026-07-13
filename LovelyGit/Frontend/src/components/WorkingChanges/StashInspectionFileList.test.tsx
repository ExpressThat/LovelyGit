// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { StashInspectionFileList } from "./StashInspectionFileList";
import type { StashInspectionFile } from "./useStashInspection";

vi.mock("@tanstack/react-virtual", () => ({
	useVirtualizer: ({ count }: { count: number }) => ({
		getTotalSize: () => count * 48,
		getVirtualItems: () =>
			Array.from({ length: count }, (_, index) => ({
				index,
				start: index * 48,
			})),
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

		expect(screen.getByText("Tracked · Modified")).toBeVisible();
		expect(screen.getByText("Untracked · Added")).toBeVisible();
		expect(screen.getByTitle("tracked.txt")).toHaveAttribute(
			"aria-current",
			"true",
		);
		await user.click(screen.getByTitle("notes.txt"));
		expect(onSelect).toHaveBeenCalledWith(untracked);
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
