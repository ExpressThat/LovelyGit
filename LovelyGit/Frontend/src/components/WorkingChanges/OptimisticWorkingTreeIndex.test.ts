import { describe, expect, it } from "vitest";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { applyOptimisticIndexMutation } from "./OptimisticWorkingTreeIndex";

describe("optimistic working-tree index changes", () => {
	it("moves one working file immediately and collapses a partial staged entry", () => {
		const changes = response({
			staged: [file("partial.ts", "Staged", "Modified")],
			unstaged: [
				file("partial.ts", "Unstaged", "Modified"),
				file("other.ts", "Unstaged", "Modified"),
			],
		});

		const next = applyOptimisticIndexMutation(
			changes,
			"stage",
			[changes.unstaged[0]],
			false,
		);

		expect(next.staged.map((entry) => entry.path)).toEqual(["partial.ts"]);
		expect(next.unstaged.map((entry) => entry.path)).toEqual(["other.ts"]);
		expect(next.totalCount).toBe(2);
		expect(changes.totalCount).toBe(3);
	});

	it("shows a newly added file as untracked when it is unstaged", () => {
		const changes = response({
			staged: [file("new.ts", "Staged", "Added")],
		});

		const next = applyOptimisticIndexMutation(
			changes,
			"unstage",
			changes.staged,
			false,
		);

		expect(next.staged).toEqual([]);
		expect(next.untracked).toEqual([
			expect.objectContaining({ group: "Untracked", path: "new.ts" }),
		]);
	});

	it("moves every eligible file while preserving genuine conflicts", () => {
		const conflict = file("conflict.ts", "Unmerged", "Modified");
		const changes = response({
			unstaged: [file("tracked.ts", "Unstaged", "Modified")],
			untracked: [file("new.ts", "Untracked", "Added")],
			unmerged: [conflict],
		});

		const next = applyOptimisticIndexMutation(changes, "stage", [], true);

		expect(next.staged.map((entry) => entry.path)).toEqual([
			"new.ts",
			"tracked.ts",
		]);
		expect(next.unmerged).toEqual([conflict]);
		expect(next.totalCount).toBe(3);
	});

	it("returns the same response when no paths are actionable", () => {
		const changes = response({
			unmerged: [file("conflict.ts", "Unmerged", "Modified")],
		});

		expect(applyOptimisticIndexMutation(changes, "stage", [], true)).toBe(
			changes,
		);
	});

	it("keeps untouched large groups stable for a single-file stage", () => {
		const staged = Array.from({ length: 20_000 }, (_, index) =>
			file(
				`staged/${index.toString().padStart(5, "0")}.ts`,
				"Staged",
				"Modified",
			),
		);
		const untracked = [file("z-new.ts", "Untracked", "Added")];
		const target = file("working/10000.ts", "Unstaged", "Modified");
		const changes = response({ staged, unstaged: [target], untracked });

		const next = applyOptimisticIndexMutation(
			changes,
			"stage",
			[target],
			false,
		);

		expect(next.untracked).toBe(untracked);
		expect(next.unmerged).toBe(changes.unmerged);
		expect(next.staged).toHaveLength(20_001);
		expect(next.staged[20_000]?.path).toBe(target.path);
		expect(next.unstaged).toEqual([]);
	});

	it("unstages a large partially-staged selection without quadratic lookup work", () => {
		const staged = Array.from({ length: 10_000 }, (_, index) =>
			file(`src/${index.toString().padStart(5, "0")}.ts`, "Staged", "Modified"),
		);
		const unstaged = staged.map((entry) => ({
			...entry,
			additions: 1,
			group: "Unstaged" as const,
		}));
		const changes = response({ staged, unstaged });
		const startedAt = performance.now();

		const next = applyOptimisticIndexMutation(changes, "unstage", [], true);
		const elapsed = performance.now() - startedAt;
		console.info(`Large optimistic unstage: ${elapsed.toFixed(2)} ms`);

		expect(next.staged).toEqual([]);
		expect(next.unstaged).toHaveLength(10_000);
		expect(next.unstaged[5_000]?.additions).toBe(1);
		expect(elapsed).toBeLessThan(50);
	});
});

function response(
	groups: Partial<
		Pick<
			WorkingTreeChangesResponse,
			"staged" | "unstaged" | "untracked" | "unmerged"
		>
	>,
): WorkingTreeChangesResponse {
	const result = {
		isComplete: true,
		staged: groups.staged ?? [],
		unstaged: groups.unstaged ?? [],
		untracked: groups.untracked ?? [],
		unmerged: groups.unmerged ?? [],
		totalCount: 0,
	};
	result.totalCount =
		result.staged.length +
		result.unstaged.length +
		result.untracked.length +
		result.unmerged.length;
	return result;
}

function file(
	path: string,
	group: WorkingTreeChangedFile["group"],
	status: WorkingTreeChangedFile["status"],
): WorkingTreeChangedFile {
	return {
		additions: 0,
		deletions: 0,
		group,
		isBinary: false,
		oldPath: null,
		path,
		status,
	};
}
