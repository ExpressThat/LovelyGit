import { describe, expect, it } from "vitest";
import type { CommitRefInfo, CommitRefKind } from "@/generated/types";
import { groupRefs, normalizeRefs } from "./RefCellUtils";

describe("commit graph ref grouping", () => {
	it("groups matching local and remote refs and keeps the current branch first", () => {
		const groups = groupRefs(
			normalizeRefs(
				[
					ref("Remote", "origin/feature"),
					ref("Local", "feature"),
					ref("Tag", "v1"),
				],
				["origin"],
			),
			["origin"],
			"feature",
		);

		expect(groups[0]).toMatchObject({
			icons: ["Local", "Remote"],
			key: "branch:feature",
		});
		expect(groups[0]?.refs).toHaveLength(2);
		expect(groups[1]?.key).toBe("tag:v1");
	});

	it("prepares a dense 500-ref commit within the interaction budget", () => {
		const refs = denseRefs();
		const startedAt = performance.now();
		let groups = groupRefs(
			normalizeRefs(refs, ["origin"]),
			["origin"],
			"feature/199",
		);
		for (let iteration = 1; iteration < 100; iteration++) {
			groups = groupRefs(
				normalizeRefs(refs, ["origin"]),
				["origin"],
				"feature/199",
			);
		}
		const elapsed = performance.now() - startedAt;
		console.info(`Dense ref grouping x100: ${elapsed.toFixed(2)} ms`);

		expect(groups).toHaveLength(300);
		expect(groups[0]?.key).toBe("branch:feature/199");
		expect(elapsed).toBeLessThan(60);
	});
});

function denseRefs() {
	const refs: CommitRefInfo[] = [];
	for (let index = 0; index < 200; index++) {
		refs.push(ref("Local", `feature/${index}`));
		refs.push(ref("Remote", `origin/feature/${index}`));
	}
	for (let index = 0; index < 100; index++) refs.push(ref("Tag", `v${index}`));
	return refs;
}

function ref(kind: CommitRefKind, name: string): CommitRefInfo {
	return { kind, name, remoteUrl: null };
}
