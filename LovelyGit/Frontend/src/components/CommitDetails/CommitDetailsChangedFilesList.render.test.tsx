// @vitest-environment jsdom

import { render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { CommitChangedFile } from "@/generated/types";
import { CommitDetailsChangedFilesList } from "./CommitDetailsChangedFilesList";

describe("CommitDetailsChangedFilesList", () => {
	it("renders ordinary visible files and their actions directly", () => {
		const onSelectFile = vi.fn();
		render(<List files={files(3)} onSelectFile={onSelectFile} />);

		const list = screen.getByRole("region", { name: "Changed files" });
		expect(list).toHaveAttribute("data-changed-files-list", "ordinary");
		expect(within(list).getAllByRole("button")).toHaveLength(3);
		within(list).getByTitle("src/file-0002.txt").click();
		expect(onSelectFile).toHaveBeenCalledWith(files(3)[2]);
	});

	it("bounds maximum commits while retaining the complete scroll range", () => {
		render(<List files={files(2_000)} />);

		const list = screen.getByRole("region", { name: "Changed files" });
		expect(list).toHaveAttribute("data-changed-files-list", "virtual");
		expect(within(list).getAllByRole("button")).toHaveLength(10);
		expect(within(list).getByTitle("src/file-0000.txt")).toBeInTheDocument();
		expect(within(list).queryByTitle("src/file-1999.txt")).toBeNull();
		expect(list.firstElementChild).toHaveStyle({ height: "84000px" });
	});
});

function List({
	files: changedFiles,
	onSelectFile = vi.fn(),
}: {
	files: CommitChangedFile[];
	onSelectFile?: (file: CommitChangedFile) => void;
}) {
	return (
		<CommitDetailsChangedFilesList
			files={changedFiles}
			hasLineStats={false}
			onOpenBlame={vi.fn()}
			onOpenHistory={vi.fn()}
			onSelectFile={onSelectFile}
		/>
	);
}

function files(count: number): CommitChangedFile[] {
	return Array.from({ length: count }, (_, index) => ({
		additions: 0,
		deletions: 0,
		isBinary: false,
		path: `src/file-${index.toString().padStart(4, "0")}.txt`,
		status: "Modified",
	}));
}
