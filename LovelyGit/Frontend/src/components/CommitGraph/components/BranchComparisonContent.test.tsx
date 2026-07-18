// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { BranchComparisonResponse } from "@/generated/types";
import { BranchComparisonContent } from "./BranchComparisonContent";

vi.mock("@tanstack/react-virtual", () => ({
	useVirtualizer: ({ count }: { count: number }) => ({
		getTotalSize: () => count * 44,
		getVirtualItems: () =>
			Array.from({ length: Math.min(count, 12) }, (_, index) => ({
				index,
				key: index,
				start: index * 44,
			})),
	}),
}));

describe("BranchComparisonContent", () => {
	it("renders the maximum file comparison payload", () => {
		const onOpenFile = vi.fn();
		const startedAt = performance.now();
		render(
			<BranchComparisonContent
				comparison={comparisonWithFiles(500)}
				onOpenFile={onOpenFile}
				section="files"
			/>,
		);
		const elapsed = performance.now() - startedAt;
		expect(screen.getAllByRole("button")).toHaveLength(12);
		expect(elapsed).toBeLessThan(65);
		expect(screen.getByText("src/generated/file-0000.ts")).toBeInTheDocument();
		expect(screen.queryByText("src/generated/file-0499.ts")).toBeNull();
		fireEvent.click(screen.getAllByRole("button")[0]);
		expect(onOpenFile).toHaveBeenCalledWith(
			expect.objectContaining({ path: "src/generated/file-0000.ts" }),
		);
	});

	it("shows the empty file state without virtual rows", () => {
		render(
			<BranchComparisonContent
				comparison={comparisonWithFiles(0)}
				section="files"
			/>,
		);

		expect(
			screen.getByText("Both branch tips have the same files."),
		).toBeInTheDocument();
		expect(screen.queryByRole("button")).toBeNull();
	});

	it("bounds the maximum commit comparison payload", () => {
		const comparison = comparisonWithFiles(0);
		comparison.aheadCommits = Array.from({ length: 100 }, (_, index) => ({
			authorName: `Author ${index}`,
			authorUnixSeconds: index,
			hash: index.toString(16).padStart(40, "0"),
			subject: `Commit subject ${index}`,
		}));
		render(<BranchComparisonContent comparison={comparison} section="ahead" />);

		expect(
			document.querySelector("[data-branch-comparison-commits='virtual']"),
		).toBeInTheDocument();
		expect(screen.getAllByRole("listitem")).toHaveLength(12);
		expect(screen.getByText("Commit subject 0")).toBeInTheDocument();
		expect(screen.queryByText("Commit subject 99")).toBeNull();
	});
});

function comparisonWithFiles(count: number): BranchComparisonResponse {
	return {
		aheadCommits: [],
		aheadCount: 0,
		behindCommits: [],
		behindCount: 0,
		changedFileCount: count,
		currentBranchName: "main",
		currentHash: "a".repeat(40),
		files: Array.from({ length: count }, (_, index) => ({
			path: `src/generated/file-${index.toString().padStart(4, "0")}.ts`,
			status: index % 2 === 0 ? "Modified" : "Added",
		})),
		isFileListTruncated: false,
		isHistoryPartial: false,
		mergeBaseHash: "c".repeat(40),
		targetBranchName: "feature",
		targetHash: "b".repeat(40),
	};
}
