import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RepositoryRefsResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import {
	clearRepositoryRefsCache,
	getCachedRepositoryRefs,
	loadRepositoryRefs,
	setCachedRepositoryRefs,
} from "./repositoryRefsCache";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));

const send = vi.mocked(sendRequestWithResponse);

describe("repositoryRefsCache", () => {
	beforeEach(() => {
		clearRepositoryRefsCache();
		send.mockReset();
	});

	it("shares in-flight and completed ref reads", async () => {
		const response = refs("main");
		let complete: (value: RepositoryRefsResponse) => void = () => undefined;
		send.mockReturnValueOnce(
			new Promise((resolve) => {
				complete = resolve;
			}),
		);

		const first = loadRepositoryRefs("repo");
		const second = loadRepositoryRefs("repo");
		expect(send).toHaveBeenCalledTimes(1);
		complete(response);

		await expect(first).resolves.toBe(response);
		await expect(second).resolves.toBe(response);
		await expect(loadRepositoryRefs("repo")).resolves.toBe(response);
		expect(send).toHaveBeenCalledTimes(1);
		expect(getCachedRepositoryRefs("repo")).toBe(response);
	});

	it("does not let an older forced request overwrite fresher refs", async () => {
		let finishOld: (value: RepositoryRefsResponse) => void = () => undefined;
		send
			.mockReturnValueOnce(new Promise((resolve) => (finishOld = resolve)))
			.mockResolvedValueOnce(refs("fresh"));

		const oldRequest = loadRepositoryRefs("repo");
		await expect(loadRepositoryRefs("repo", true)).resolves.toEqual(
			refs("fresh"),
		);
		finishOld(refs("stale"));
		await oldRequest;

		await expect(loadRepositoryRefs("repo")).resolves.toEqual(refs("fresh"));
	});

	it("removes failed entries so a retry can succeed", async () => {
		send
			.mockRejectedValueOnce(new Error("failed"))
			.mockResolvedValueOnce(refs("retry"));

		await expect(loadRepositoryRefs("repo")).rejects.toThrow("failed");
		await expect(loadRepositoryRefs("repo")).resolves.toEqual(refs("retry"));
		expect(send).toHaveBeenCalledTimes(2);
	});

	it("retains refs for eight recently used repositories", () => {
		for (const repositoryId of ["a", "b", "c", "d", "e", "f", "g", "h"]) {
			setCachedRepositoryRefs(repositoryId, refs(repositoryId));
		}

		expect(getCachedRepositoryRefs("a")?.currentBranchName).toBe("a");
		setCachedRepositoryRefs("i", refs("i"));

		expect(getCachedRepositoryRefs("b")).toBeNull();
		expect(getCachedRepositoryRefs("a")?.currentBranchName).toBe("a");
	});

	it("evicts old ref sets when their combined item weight is excessive", () => {
		setCachedRepositoryRefs("old", refs("old", 2_500));
		setCachedRepositoryRefs("new", refs("new", 2_500));

		expect(getCachedRepositoryRefs("old")).toBeNull();
		expect(getCachedRepositoryRefs("new")?.refs).toHaveLength(2_500);
	});
});

function refs(branch: string, refCount = 0): RepositoryRefsResponse {
	return {
		branchUpstreams: [],
		currentBranchName: branch,
		refs: Array.from({ length: refCount }, (_, index) => ({
			commitHash: `${index}`,
			kind: "Local",
			name: `ref-${index}`,
			remoteUrl: null,
		})),
		remotePrefixes: [],
		stashes: [],
		worktrees: [],
	};
}
