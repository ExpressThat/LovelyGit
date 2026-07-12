import { beforeEach, describe, expect, it, vi } from "vitest";
import type { CommitDetailsResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import {
	clearCommitDetailsCache,
	getCachedCommitDetails,
	loadCommitDetails,
} from "./commitDetailsCache";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));

const send = vi.mocked(sendRequestWithResponse);

describe("commitDetailsCache", () => {
	beforeEach(() => {
		clearCommitDetailsCache();
		send.mockReset();
	});

	it("shares in-flight and completed detail reads", async () => {
		const value = details(1);
		let complete: (response: CommitDetailsResponse) => void = () => undefined;
		send.mockReturnValueOnce(new Promise((resolve) => (complete = resolve)));

		const first = loadCommitDetails("repo", "hash", 0);
		const second = loadCommitDetails("repo", "hash", 0);
		expect(send).toHaveBeenCalledTimes(1);
		complete(value);

		await expect(first).resolves.toBe(value);
		await expect(second).resolves.toBe(value);
		await expect(loadCommitDetails("repo", "hash", 0)).resolves.toBe(value);
		expect(send).toHaveBeenCalledTimes(1);
	});

	it("exposes only completed responses with repository and parent isolation", async () => {
		let complete: (response: CommitDetailsResponse) => void = () => undefined;
		send.mockReturnValueOnce(new Promise((resolve) => (complete = resolve)));
		const pending = loadCommitDetails("repo", "hash", 1);
		expect(getCachedCommitDetails("repo", "hash", 1)).toBeUndefined();

		const value = details(2);
		complete(value);
		await pending;
		expect(getCachedCommitDetails("repo", "hash", 1)).toBe(value);
		expect(getCachedCommitDetails("other", "hash", 1)).toBeUndefined();
		expect(getCachedCommitDetails("repo", "hash", 0)).toBeUndefined();
	});

	it("retains one oversized response for instant selection", async () => {
		send.mockResolvedValue(details(101));

		await loadCommitDetails("repo", "large-a", 0);
		await loadCommitDetails("repo", "large-a", 0);
		expect(send).toHaveBeenCalledTimes(1);

		await loadCommitDetails("repo", "large-b", 0);
		await loadCommitDetails("repo", "large-b", 0);
		expect(send).toHaveBeenCalledTimes(2);

		await loadCommitDetails("repo", "large-a", 0);
		expect(send).toHaveBeenCalledTimes(3);
	});

	it("clears failures so selection can retry", async () => {
		send
			.mockRejectedValueOnce(new Error("failed"))
			.mockResolvedValueOnce(details(1));

		await expect(loadCommitDetails("repo", "hash", 0)).rejects.toThrow(
			"failed",
		);
		await expect(loadCommitDetails("repo", "hash", 0)).resolves.toEqual(
			details(1),
		);
	});
});

function details(fileCount: number): CommitDetailsResponse {
	return {
		author: "Ada",
		body: "",
		branches: [],
		changedFiles: Array.from({ length: fileCount }, (_, index) => ({
			additions: 1,
			deletions: 0,
			isBinary: false,
			path: `${index}.txt`,
			status: "A",
		})),
		date: 1,
		email: "ada@example.test",
		hash: "a".repeat(40),
		message: "Commit",
		parents: [],
		signatureKind: "None",
		stats: { additions: fileCount, deletions: 0 },
		subject: "Commit",
		tags: [],
	};
}
