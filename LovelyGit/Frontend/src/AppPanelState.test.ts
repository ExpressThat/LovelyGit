import { describe, expect, it } from "vitest";
import { commitDetailsPanel, panelTitle } from "./AppPanelState";

describe("AppPanelState", () => {
	it("preserves merge-parent context through file drill-in", () => {
		const file = {
			additions: 1,
			deletions: 0,
			isBinary: false,
			path: "main.txt",
			status: "A",
		};

		const panel = commitDetailsPanel("merge", 1, file);

		expect(panel).toEqual({
			commitHash: "merge",
			kind: "commit",
			parentIndex: 1,
			selectedFile: file,
		});
		expect(panelTitle(panel)).toBe("Commit Details");
	});
});
