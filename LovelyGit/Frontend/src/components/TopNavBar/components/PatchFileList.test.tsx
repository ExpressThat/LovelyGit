// @vitest-environment jsdom

import { render, screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { PatchFileList } from "./PatchFileList";

function createFiles(count: number) {
	return Array.from({ length: count }, (_, index) => ({
		additions: index + 1,
		deletions: index,
		path: `src/file-${index.toString().padStart(4, "0")}.ts`,
	}));
}

describe("PatchFileList", () => {
	it("keeps ordinary previews fully mounted", () => {
		render(<PatchFileList files={createFiles(3)} />);

		expect(
			screen.getByRole("list", { name: "Patch files" }),
		).not.toHaveAttribute("data-patch-file-list");
		expect(screen.getAllByRole("listitem")).toHaveLength(3);
		expect(screen.getByText("src/file-0002.ts")).toBeInTheDocument();
	});

	it("bounds maximum previews while retaining their complete scroll range", () => {
		render(<PatchFileList files={createFiles(5_000)} />);

		const list = screen.getByRole("list", { name: "Patch files" });
		expect(list).toHaveAttribute("data-patch-file-list", "virtual");
		expect(within(list).getAllByRole("listitem")).toHaveLength(9);
		expect(within(list).getByText("src/file-0000.ts")).toBeInTheDocument();
		expect(list).toHaveStyle({ height: "160000px" });
	});
});
