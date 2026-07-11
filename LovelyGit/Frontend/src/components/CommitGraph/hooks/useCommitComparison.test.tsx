// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { useCommitComparison } from "./useCommitComparison";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
const send = vi.mocked(sendRequestWithResponse);

describe("useCommitComparison", () => {
	beforeEach(() => send.mockReset());

	it("requests exact commit identities", async () => {
		send.mockResolvedValueOnce({ changedFileCount: 1 });
		const { result } = renderHook(() =>
			useCommitComparison("repo", "a".repeat(40), "b".repeat(40)),
		);
		await waitFor(() => expect(result.current.comparison).not.toBeNull());
		expect(send).toHaveBeenCalledWith({
			arguments: {
				currentCommitHash: "a".repeat(40),
				repositoryId: "repo",
				targetBranchName: "",
				targetCommitHash: "b".repeat(40),
			},
			commandType: "GetBranchComparison",
		});
	});

	it("surfaces failure and retries without losing a prior result", async () => {
		send
			.mockResolvedValueOnce({ changedFileCount: 1 })
			.mockRejectedValueOnce(new Error("object missing"))
			.mockResolvedValueOnce({ changedFileCount: 2 });
		const { result, rerender } = renderHook(
			({ target }) => useCommitComparison("repo", "a".repeat(40), target),
			{ initialProps: { target: "b".repeat(40) } },
		);
		await waitFor(() =>
			expect(result.current.comparison?.changedFileCount).toBe(1),
		);
		rerender({ target: "c".repeat(40) });
		await waitFor(() => expect(result.current.error).toBe("object missing"));
		expect(result.current.comparison?.changedFileCount).toBe(1);
		act(() => result.current.retry());
		await waitFor(() =>
			expect(result.current.comparison?.changedFileCount).toBe(2),
		);
	});
});
