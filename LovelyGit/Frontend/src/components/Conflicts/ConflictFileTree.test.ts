import { describe, expect, it } from "vitest";
import type { GitConflictFile } from "@/generated/types";
import { buildConflictFileTree } from "./ConflictFileTree";

describe("buildConflictFileTree", () => {
	it("groups conflict files by directory and sorts directories first", () => {
		const tree = buildConflictFileTree([
			file("zeta.ts"),
			file("src/components/button.tsx"),
			file("src/App.tsx"),
			file("README.md"),
		]);

		expect(tree).toMatchObject([
			{
				name: "src",
				type: "directory",
				children: [
					{
						name: "components",
						type: "directory",
						children: [{ name: "button.tsx", type: "file" }],
					},
					{ name: "App.tsx", type: "file" },
				],
			},
			{ name: "README.md", type: "file" },
			{ name: "zeta.ts", type: "file" },
		]);
	});
});

function file(path: string): GitConflictFile {
	return {
		conflictCount: 1,
		isBinary: false,
		path,
		status: "Unmerged",
	};
}
