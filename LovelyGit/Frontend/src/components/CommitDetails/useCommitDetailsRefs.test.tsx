// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RepositoryRefsResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import {
	clearRepositoryRefsCache,
	setCachedRepositoryRefs,
} from "@/lib/repositoryRefsCache";
import { useCommitDetailsRefs } from "./useCommitDetailsRefs";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
const send = vi.mocked(sendRequestWithResponse);

describe("useCommitDetailsRefs", () => {
	beforeEach(() => {
		clearRepositoryRefsCache();
		send.mockReset();
	});

	it("loads and derives current non-stash refs for the selected commit", async () => {
		send.mockResolvedValueOnce(
			response([
				ref("Local", "main", "target"),
				ref("Remote", "origin/main", "target"),
				ref("Tag", "v1", "target"),
				ref("Stash", "stash@{0}", "target"),
				ref("Local", "other", "elsewhere"),
			]),
		);

		const { result } = renderHook(() => useCommitDetailsRefs("repo", "target"));
		await waitFor(() => expect(result.current).toHaveLength(3));
		expect(result.current.map((item) => item.name)).toEqual([
			"main",
			"origin/main",
			"v1",
		]);
		expect(send).toHaveBeenCalledOnce();
	});

	it("updates immediately when the shared ref snapshot changes", () => {
		setCachedRepositoryRefs("repo", response([ref("Local", "old", "target")]));
		const { result } = renderHook(() => useCommitDetailsRefs("repo", "target"));

		expect(result.current.map((item) => item.name)).toEqual(["old"]);
		act(() => {
			setCachedRepositoryRefs(
				"repo",
				response([ref("Local", "new", "target")]),
			);
		});
		expect(result.current.map((item) => item.name)).toEqual(["new"]);
		expect(send).not.toHaveBeenCalled();
	});

	it("leaves the surface empty when ref loading fails", async () => {
		send.mockRejectedValueOnce(new Error("refs unavailable"));
		const { result } = renderHook(() => useCommitDetailsRefs("repo", "target"));

		await waitFor(() => expect(send).toHaveBeenCalledOnce());
		expect(result.current).toEqual([]);
	});
});

function ref(
	kind: "Local" | "Remote" | "Stash" | "Tag",
	name: string,
	commitHash: string,
) {
	return { commitHash, kind, name, remoteUrl: null };
}

function response(refs: ReturnType<typeof ref>[]): RepositoryRefsResponse {
	return {
		branchUpstreams: [],
		compactRefsGzipBase64: null,
		currentBranchName: "main",
		refs,
		remotePrefixes: ["origin"],
		stashes: [],
		worktrees: [],
	};
}
