import { describe, expect, it } from "vitest";
import type { RepositoryRefsResponse } from "@/generated/types";
import {
	branchTrackingMetadata,
	refCommitHash,
	withBranchUpstream,
	withLocalBranchChange,
} from "./refMetadata";

describe("branchTrackingMetadata", () => {
	it("maps upstreams and excludes symbolic remote HEAD refs", () => {
		const response = {
			refs: [
				{ kind: "Remote", name: "origin/HEAD" },
				{ kind: "Remote", name: "origin/main" },
				{ kind: "Local", name: "main" },
			],
			branchUpstreams: [{ branchName: "main", upstreamName: "origin/main" }],
		} as RepositoryRefsResponse;

		expect(branchTrackingMetadata(response)).toEqual({
			remoteBranchNames: ["origin/main"],
			upstreams: { main: "origin/main" },
		});
	});

	it("returns empty metadata before refs load", () => {
		expect(branchTrackingMetadata(null)).toEqual({
			remoteBranchNames: [],
			upstreams: {},
		});
	});
});

describe("refCommitHash", () => {
	it("finds the matching named ref", () => {
		const response = {
			refs: [{ commitHash: "abc123", kind: "Local", name: "main" }],
		} as RepositoryRefsResponse;

		expect(refCommitHash(response, "Local", "main")).toBe("abc123");
		expect(refCommitHash(response, "Local", "missing")).toBeNull();
	});
});

describe("withBranchUpstream", () => {
	it("replaces and removes tracking metadata without mutating the response", () => {
		const response = {
			branchUpstreams: [{ branchName: "main", upstreamName: "origin/main" }],
		} as RepositoryRefsResponse;

		const replaced = withBranchUpstream(response, "main", "upstream/main");
		const removed = withBranchUpstream(replaced, "main", null);

		expect(replaced.branchUpstreams).toEqual([
			{ branchName: "main", upstreamName: "upstream/main" },
		]);
		expect(removed.branchUpstreams).toEqual([]);
		expect(response.branchUpstreams).toEqual([
			{ branchName: "main", upstreamName: "origin/main" },
		]);
	});
});

describe("withLocalBranchChange", () => {
	it("renames and removes local refs with their tracking metadata", () => {
		const response = {
			branchUpstreams: [{ branchName: "main", upstreamName: "origin/main" }],
			currentBranchName: "main",
			refs: [{ commitHash: "abc", kind: "Local", name: "main" }],
		} as RepositoryRefsResponse;

		const renamed = withLocalBranchChange(response, "main", "trunk");
		expect(renamed.currentBranchName).toBe("trunk");
		expect(renamed.refs[0]?.name).toBe("trunk");
		expect(renamed.branchUpstreams).toEqual([
			{ branchName: "trunk", upstreamName: "origin/main" },
		]);

		const removed = withLocalBranchChange(renamed, "trunk", null);
		expect(removed.refs).toEqual([]);
		expect(removed.branchUpstreams).toEqual([]);
	});
});
