// @vitest-environment jsdom

import { renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { GitReflogEntry } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { filterReflogEntries, useReflog } from "./useReflog";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));

const entry = reflogEntry();

describe("useReflog", () => {
	beforeEach(() => vi.clearAllMocks());

	it("loads a bounded native branch reflog", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce({
			entries: [entry],
			referenceName: "main",
		});
		const { result } = renderHook(() => useReflog("repo", "main"));

		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.entries).toEqual([entry]);
		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: { branchName: "main", knownRepositoryId: "repo", limit: 200 },
			commandType: "GetReflog",
		});
	});

	it("surfaces native read errors", async () => {
		vi.mocked(sendRequestWithResponse).mockRejectedValueOnce(
			new Error("reflog unavailable"),
		);
		const { result } = renderHook(() => useReflog("repo", "main"));

		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.error).toBe("reflog unavailable");
		expect(result.current.entries).toEqual([]);
	});
});

describe("filterReflogEntries", () => {
	it("matches selector, hashes, actor, email, and message", () => {
		for (const query of [
			"main@{0}",
			"abcdef",
			"Ross",
			"ross@example",
			"reset",
		]) {
			expect(filterReflogEntries([entry], query)).toEqual([entry]);
		}
		expect(filterReflogEntries([entry], "missing")).toEqual([]);
	});
});

function reflogEntry(): GitReflogEntry {
	return {
		actorEmail: "ross@example.invalid",
		actorName: "Ross",
		message: "reset: moving to HEAD~1",
		newHash: "abcdef1234567890abcdef1234567890abcdef12",
		oldHash: "1234567890abcdef1234567890abcdef12345678",
		selector: "main@{0}",
		timestampUnixSeconds: 1_700_000_000,
		timezone: "+0000",
	};
}
