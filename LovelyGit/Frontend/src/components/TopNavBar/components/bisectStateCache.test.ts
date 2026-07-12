import { beforeEach, describe, expect, it } from "vitest";
import type { GitBisectState } from "@/generated/types";
import {
	clearBisectStateCache,
	getCachedBisectState,
	setCachedBisectState,
} from "./bisectStateCache";

describe("bisectStateCache", () => {
	beforeEach(clearBisectStateCache);

	it("stores state by repository", () => {
		const value = state("main");
		setCachedBisectState("repo", value);

		expect(getCachedBisectState("repo")).toBe(value);
		expect(getCachedBisectState("missing")).toBeNull();
	});

	it("evicts the least recently used repository", () => {
		for (const name of ["a", "b", "c", "d"]) {
			setCachedBisectState(name, state(name));
		}
		expect(getCachedBisectState("a")?.startingReference).toBe("a");
		setCachedBisectState("e", state("e"));

		expect(getCachedBisectState("b")).toBeNull();
		expect(getCachedBisectState("a")?.startingReference).toBe("a");
	});
});

function state(startingReference: string): GitBisectState {
	return {
		badCommit: null,
		currentCommit: null,
		currentSubject: null,
		firstBadCommit: null,
		goodCommits: [],
		isActive: false,
		startingReference,
	};
}
