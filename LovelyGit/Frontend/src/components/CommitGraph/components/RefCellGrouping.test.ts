import { describe, expect, it } from "vitest";
import type { CommitRefInfo } from "@/generated/types";
import { buildRefCellGroupView } from "./RefCellGrouping";
import type { RefGroup } from "./RefCellUtils";

describe("buildRefCellGroupView", () => {
	it("returns an empty view when a commit has no refs", () => {
		expect(buildRefCellGroupView([])).toEqual({
			hiddenCount: 0,
			visibleGroups: [],
		});
	});

	it("keeps only the primary logical ref visible and counts every extra ref", () => {
		const view = buildRefCellGroupView([
			group("main", [ref("Local", "main"), ref("Remote", "origin/main")]),
			group("feature", [ref("Local", "feature")]),
			group("v1", [ref("Tag", "v1")]),
		]);

		expect(view.visibleGroups.map((group) => group.key)).toEqual(["main"]);
		expect(view.hiddenCount).toBe(2);
		expect(view.visibleGroups[0].refs.map((ref) => ref.name)).toEqual([
			"main",
			"origin/main",
		]);
	});

	it("does not show a count for a single reference", () => {
		const view = buildRefCellGroupView([group("v1", [ref("Tag", "v1")])]);

		expect(view.visibleGroups).toHaveLength(1);
		expect(view.hiddenCount).toBe(0);
	});
});

function group(key: string, refs: CommitRefInfo[]): RefGroup {
	return { icons: refs.map((ref) => ref.kind), key, primary: refs[0], refs };
}

function ref(kind: CommitRefInfo["kind"], name: string): CommitRefInfo {
	return { kind, name, remoteUrl: null };
}
