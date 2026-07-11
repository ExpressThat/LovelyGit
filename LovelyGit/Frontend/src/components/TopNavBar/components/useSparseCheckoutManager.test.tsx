// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { toast } from "sonner";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { useSparseCheckoutManager } from "./useSparseCheckoutManager";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({ toast: { error: vi.fn(), success: vi.fn() } }));

describe("useSparseCheckoutManager", () => {
	beforeEach(() => vi.clearAllMocks());

	it("loads native sparse-checkout state", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValue({
			coneMode: true,
			enabled: true,
			patterns: ["src"],
		});
		const { result } = renderHook(() => useSparseCheckoutManager("repo"));

		await act(() => result.current.load());

		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: { repositoryId: "repo" },
			commandType: "GetSparseCheckoutState",
		});
		expect(result.current.state?.patterns).toEqual(["src"]);
	});

	it("shows a retryable native read failure", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockRejectedValueOnce(new Error("Config unavailable"))
			.mockResolvedValueOnce({ coneMode: false, enabled: false, patterns: [] });
		const { result } = renderHook(() => useSparseCheckoutManager("repo"));

		await act(() => result.current.load());
		expect(result.current.error).toBe("Config unavailable");
		await act(() => result.current.load());

		expect(result.current.error).toBeNull();
		expect(result.current.state?.enabled).toBe(false);
	});

	it("updates selections and recovers after a mutation failure", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockRejectedValueOnce(new Error("Local changes would be overwritten"))
			.mockResolvedValueOnce({
				coneMode: true,
				enabled: true,
				patterns: ["apps/desktop"],
			});
		const { result } = renderHook(() => useSparseCheckoutManager("repo"));

		await act(() => result.current.run("Set", true, ["apps/desktop"]));
		expect(toast.error).toHaveBeenCalledWith(
			"Local changes would be overwritten",
			{ duration: 8_000 },
		);
		await act(() => result.current.run("Set", true, ["apps/desktop"]));

		expect(sendRequestWithResponse).toHaveBeenLastCalledWith(
			{
				arguments: {
					action: "Set",
					coneMode: true,
					patterns: ["apps/desktop"],
					repositoryId: "repo",
				},
				commandType: "ManageSparseCheckout",
			},
			{ timeoutMs: 120_000 },
		);
		await waitFor(() => expect(result.current.busyAction).toBeNull());
		expect(result.current.state?.enabled).toBe(true);
	});
});
