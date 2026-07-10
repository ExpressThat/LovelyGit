// @vitest-environment jsdom
import { act, renderHook, waitFor } from "@testing-library/react";
import { toast } from "sonner";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "@/lib/commands";
import { useBisectSession } from "./useBisectSession";

vi.mock("@/lib/commands", () => ({
	sendRequestWithResponse: vi.fn(),
	subscribeToServerEvent: vi.fn(() => vi.fn()),
}));
vi.mock("sonner", () => ({
	toast: { error: vi.fn(), loading: vi.fn(() => "toast"), success: vi.fn() },
}));

const activeState = {
	badCommit: "b".repeat(40),
	currentCommit: "c".repeat(40),
	currentSubject: "Midpoint",
	firstBadCommit: null,
	goodCommits: ["a".repeat(40)],
	isActive: true,
	startingReference: "main",
};

describe("useBisectSession", () => {
	beforeEach(() => vi.clearAllMocks());

	it("loads native state and marks the current revision good", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValue(activeState);
		const { result } = renderHook(() => useBisectSession("repo"));
		await waitFor(() => expect(result.current.state).toEqual(activeState));

		await act(() => result.current.run("MarkGood"));

		expect(sendRequestWithResponse).toHaveBeenLastCalledWith(
			{
				arguments: {
					action: "MarkGood",
					goodCommit: null,
					repositoryId: "repo",
				},
				commandType: "ManageBisect",
			},
			{ timeoutMs: 30_000 },
		);
		expect(subscribeToServerEvent).toHaveBeenCalledWith(
			"CommitGraphChanged",
			expect.any(Function),
		);
	});

	it("surfaces initial load failure and can load successfully on retry", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockRejectedValueOnce(new Error("Repository disappeared"))
			.mockResolvedValueOnce(activeState);
		const { result } = renderHook(() => useBisectSession("repo"));

		await waitFor(() => expect(result.current.isLoading).toBe(false));
		expect(result.current.state).toBeNull();
		expect(toast.error).toHaveBeenCalledWith("Repository disappeared");

		await act(() => result.current.load());
		expect(result.current.state).toEqual(activeState);
	});

	it("failed progression preserves state, re-enables controls, and permits retry", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockResolvedValueOnce(activeState)
			.mockRejectedValueOnce(new Error("Git bisect failed"))
			.mockResolvedValueOnce(activeState);
		const { result } = renderHook(() => useBisectSession("repo"));
		await waitFor(() => expect(result.current.state).toEqual(activeState));

		await act(() => result.current.run("MarkBad"));

		expect(result.current.state).toEqual(activeState);
		expect(result.current.busyAction).toBeNull();
		expect(toast.error).toHaveBeenCalledWith("Git bisect failed", {
			id: "toast",
		});

		await act(() => result.current.run("MarkBad"));
		expect(sendRequestWithResponse).toHaveBeenCalledTimes(3);
		expect(toast.success).toHaveBeenCalled();
	});
});
