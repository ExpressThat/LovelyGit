// @vitest-environment jsdom

import { renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	getCachedRepositoryRefs,
	loadRepositoryRefs,
} from "@/lib/repositoryRefsCache";
import { useCommitSearchRefs } from "./useCommitSearchRefs";

vi.mock("@/lib/repositoryRefsCache", () => ({
	getCachedRepositoryRefs: vi.fn(),
	loadRepositoryRefs: vi.fn(),
}));

describe("useCommitSearchRefs", () => {
	beforeEach(() => vi.clearAllMocks());

	it("reuses cached refs without issuing a request", () => {
		vi.mocked(getCachedRepositoryRefs).mockReturnValue(response());
		const { result } = renderHook(() => useCommitSearchRefs("repo", true));

		expect(result.current.refs[0]?.name).toBe("main");
		expect(loadRepositoryRefs).not.toHaveBeenCalled();
	});

	it("loads lazily and leaves a retryable empty state after failure", async () => {
		vi.mocked(getCachedRepositoryRefs).mockReturnValue(null);
		vi.mocked(loadRepositoryRefs).mockRejectedValue(new Error("read failed"));
		const { result } = renderHook(() => useCommitSearchRefs("repo", true));

		expect(result.current.isLoading).toBe(true);
		await waitFor(() => expect(result.current.isLoading).toBe(false));
		expect(result.current.refs).toEqual([]);
		expect(result.current.loadFailed).toBe(true);
	});
});

function response() {
	return {
		branchUpstreams: [],
		currentBranchName: "main",
		refs: [
			{
				commitHash: "a".repeat(40),
				kind: "Local" as const,
				name: "main",
				remoteUrl: null,
			},
		],
		remotePrefixes: [],
		stashes: [],
		worktrees: [],
	};
}
