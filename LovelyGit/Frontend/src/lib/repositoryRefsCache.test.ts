import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RepositoryRefsResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import {
	clearRepositoryRefsCache,
	getCachedRepositoryRefs,
	loadRepositoryRefs,
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
});

function refs(branch: string): RepositoryRefsResponse {
	return {
		branchUpstreams: [],
		currentBranchName: branch,
		refs: [],
		remotePrefixes: [],
		stashes: [],
		worktrees: [],
	};
}
