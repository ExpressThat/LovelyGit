// @vitest-environment jsdom
import { act, renderHook, waitFor } from "@testing-library/react";
import { toast } from "sonner";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "@/lib/commands";
import {
	clearBisectStateCache,
	getCachedBisectState,
	setCachedBisectState,
} from "./bisectStateCache";
import {
	CACHED_BISECT_REFRESH_DELAY_MS,
	useBisectSession,
} from "./useBisectSession";

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
	beforeEach(() => {
		clearBisectStateCache();
		vi.clearAllMocks();
	});

	afterEach(() => vi.useRealTimers());

	it("shows cached state immediately and defers revalidation", async () => {
		vi.useFakeTimers();
		setCachedBisectState("repo", activeState);
		vi.mocked(sendRequestWithResponse).mockResolvedValue(activeState);
		const { result } = renderHook(() => useBisectSession("repo"));

		expect(result.current.state).toBe(activeState);
		expect(result.current.isLoading).toBe(false);
		expect(sendRequestWithResponse).not.toHaveBeenCalled();
		act(() => vi.advanceTimersByTime(CACHED_BISECT_REFRESH_DELAY_MS));
		await act(async () => Promise.resolve());

		expect(sendRequestWithResponse).toHaveBeenCalledOnce();
	});

	it("cancels an abandoned cached refresh on a rapid tab switch", () => {
		vi.useFakeTimers();
		setCachedBisectState("repo-a", activeState);
		setCachedBisectState("repo-b", activeState);
		vi.mocked(sendRequestWithResponse).mockResolvedValue(activeState);
		const { rerender } = renderHook(
			({ repositoryId }) => useBisectSession(repositoryId),
			{ initialProps: { repositoryId: "repo-a" } },
		);

		rerender({ repositoryId: "repo-b" });
		act(() => vi.advanceTimersByTime(CACHED_BISECT_REFRESH_DELAY_MS));

		expect(sendRequestWithResponse).toHaveBeenCalledOnce();
		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			commandType: "GetBisectState",
			arguments: { repositoryId: "repo-b" },
		});
	});

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
		expect(getCachedBisectState("repo")).toEqual(activeState);
	});

	it("refreshes immediately after a graph invalidation", () => {
		let graphChanged: (() => void) | undefined;
		vi.useFakeTimers();
		setCachedBisectState("repo", activeState);
		vi.mocked(subscribeToServerEvent).mockImplementation((_event, listener) => {
			graphChanged = listener as () => void;
			return vi.fn();
		});
		vi.mocked(sendRequestWithResponse).mockResolvedValue(activeState);
		renderHook(() => useBisectSession("repo"));

		act(() => graphChanged?.());

		expect(sendRequestWithResponse).toHaveBeenCalledOnce();
		act(() => vi.advanceTimersByTime(CACHED_BISECT_REFRESH_DELAY_MS));
		expect(sendRequestWithResponse).toHaveBeenCalledOnce();
	});

	it("surfaces delayed load failure and can load successfully on retry", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockRejectedValueOnce(new Error("Repository disappeared"))
			.mockResolvedValueOnce(activeState);
		const { result } = renderHook(() => useBisectSession("repo"));

		await waitFor(() =>
			expect(toast.error).toHaveBeenCalledWith("Repository disappeared"),
		);
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
