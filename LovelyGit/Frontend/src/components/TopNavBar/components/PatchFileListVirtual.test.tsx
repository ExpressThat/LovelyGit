// @vitest-environment jsdom

import { render, screen, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { PatchFileList } from "./PatchFileList";

let showLastRow = false;

vi.mock("@tanstack/react-virtual", () => ({
	useVirtualizer: ({ count }: { count: number }) => ({
		getTotalSize: () => count * 32,
		getVirtualItems: () =>
			showLastRow ? [{ index: count - 1, start: (count - 1) * 32 }] : [],
	}),
}));

describe("virtual patch file navigation", () => {
	beforeEach(() => {
		showLastRow = false;
	});

	it("can replace the bootstrap rows with the final file", () => {
		const files = Array.from({ length: 5_000 }, (_, index) => ({
			additions: 1,
			deletions: 0,
			path: `src/file-${index.toString().padStart(4, "0")}.ts`,
		}));
		const view = render(<PatchFileList files={files} />);
		const list = screen.getByRole("list", { name: "Patch files" });

		expect(within(list).getByText("src/file-0000.ts")).toBeInTheDocument();
		showLastRow = true;
		view.rerender(<PatchFileList files={[...files]} />);

		expect(within(list).getByText("src/file-4999.ts")).toBeInTheDocument();
		expect(
			within(list).queryByText("src/file-0000.ts"),
		).not.toBeInTheDocument();
	});
});
