// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { toast } from "sonner";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { useLfsManager } from "./useLfsManager";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({ toast: { error: vi.fn(), success: vi.fn() } }));

const initialState = {
	hasTrackedPatterns: true,
	isAvailable: true,
	isInitialized: true,
	trackedPatterns: ["*.psd"],
};

describe("useLfsManager", () => {
	beforeEach(() => vi.clearAllMocks());

	it("loads native LFS state", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValue(initialState);
		const { result } = renderHook(() => useLfsManager("repository-id"));

		await act(() => result.current.load());

		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: { repositoryId: "repository-id" },
			commandType: "GetGitLfsState",
		});
		expect(result.current.state).toEqual(initialState);
	});

	it("uses the returned state after tracking a pattern", async () => {
		const tracked = {
			...initialState,
			trackedPatterns: ["*.psd", "Assets/**"],
		};
		vi.mocked(sendRequestWithResponse).mockResolvedValue(tracked);
		const { result } = renderHook(() => useLfsManager("repository-id"));

		await act(() => result.current.run("Track", "Assets/**"));

		expect(sendRequestWithResponse).toHaveBeenCalledWith(
			{
				arguments: {
					action: "Track",
					pattern: "Assets/**",
					repositoryId: "repository-id",
				},
				commandType: "ManageGitLfs",
			},
			{ timeoutMs: 120_000 },
		);
		expect(result.current.state).toEqual(tracked);
		expect(toast.success).toHaveBeenCalledWith("LFS pattern tracked");
	});

	it("preserves state after failure, re-enables controls, and permits retry", async () => {
		const updated = { ...initialState, trackedPatterns: [] };
		vi.mocked(sendRequestWithResponse)
			.mockResolvedValueOnce(initialState)
			.mockRejectedValueOnce(new Error("LFS command failed"))
			.mockResolvedValueOnce(updated);
		const { result } = renderHook(() => useLfsManager("repository-id"));
		await act(() => result.current.load());

		await act(() => result.current.run("Untrack", "*.psd"));
		expect(result.current.state).toEqual(initialState);
		expect(result.current.busyAction).toBeNull();
		expect(toast.error).toHaveBeenCalledWith("LFS command failed", {
			duration: 8_000,
		});

		await act(() => result.current.run("Untrack", "*.psd"));
		expect(result.current.state).toEqual(updated);
		expect(toast.success).toHaveBeenCalledWith("LFS pattern removed");
	});

	it("surfaces a load failure and allows a successful retry", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockRejectedValueOnce(new Error("Unreadable attributes"))
			.mockResolvedValueOnce(initialState);
		const { result } = renderHook(() => useLfsManager("repository-id"));

		await act(() => result.current.load());
		expect(result.current.error).toBe("Unreadable attributes");
		expect(result.current.isLoading).toBe(false);

		await act(() => result.current.load());
		expect(result.current.error).toBeNull();
		expect(result.current.state).toEqual(initialState);
	});
});
