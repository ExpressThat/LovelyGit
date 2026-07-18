// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { toast } from "sonner";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	decodeGzipBase64,
	encodeGzipBase64,
} from "@/components/CommitFileDiff/compactPayloadCompression";
import { sendRequestWithResponse } from "@/lib/commands";
import { useSparseCheckoutManager } from "./useSparseCheckoutManager";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("@/components/CommitFileDiff/compactPayloadCompression", () => ({
	decodeGzipBase64: vi.fn(),
	encodeGzipBase64: vi.fn(),
}));
vi.mock("sonner", () => ({ toast: { error: vi.fn(), success: vi.fn() } }));

describe("useSparseCheckoutManager", () => {
	beforeEach(() => vi.clearAllMocks());

	it("loads native sparse-checkout state", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValue({
			coneMode: true,
			enabled: true,
			patternCount: 1,
			patternText: "src",
			patternTextGzipBase64: "",
		});
		const { result } = renderHook(() => useSparseCheckoutManager("repo"));

		await act(() => result.current.load());

		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: { repositoryId: "repo" },
			commandType: "GetSparseCheckoutState",
		});
		expect(result.current.state?.patternText).toBe("src");
	});

	it("expands a compact native specification", async () => {
		vi.mocked(decodeGzipBase64).mockResolvedValue("src\ndocs");
		vi.mocked(sendRequestWithResponse).mockResolvedValue({
			coneMode: false,
			enabled: true,
			patternCount: 2,
			patternText: "",
			patternTextGzipBase64: "compact-patterns",
		});
		const { result } = renderHook(() => useSparseCheckoutManager("repo"));

		await act(() => result.current.load());

		expect(decodeGzipBase64).toHaveBeenCalledWith("compact-patterns");
		expect(result.current.state?.patternText).toBe("src\ndocs");
		expect(result.current.state?.patternTextGzipBase64).toBe("");
	});

	it("shows a retryable native read failure", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockRejectedValueOnce(new Error("Config unavailable"))
			.mockResolvedValueOnce({
				coneMode: false,
				enabled: false,
				patternCount: 0,
				patternText: "",
				patternTextGzipBase64: "",
			});
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
				patternCount: 1,
				patternText: "apps/desktop",
				patternTextGzipBase64: "",
			});
		const { result } = renderHook(() => useSparseCheckoutManager("repo"));

		await act(() => result.current.run("Set", true, "apps/desktop"));
		expect(toast.error).toHaveBeenCalledWith(
			"Local changes would be overwritten",
			{ duration: 8_000 },
		);
		await act(() => result.current.run("Set", true, "apps/desktop"));

		expect(sendRequestWithResponse).toHaveBeenLastCalledWith(
			{
				arguments: {
					action: "Set",
					coneMode: true,
					patternText: "apps/desktop",
					patternTextGzipBase64: "",
					repositoryId: "repo",
				},
				commandType: "ManageSparseCheckout",
			},
			{ timeoutMs: 120_000 },
		);
		await waitFor(() => expect(result.current.busyAction).toBeNull());
		expect(result.current.state?.enabled).toBe(true);
	});

	it("compresses a large specification before native transport", async () => {
		const patternText = "path\n".repeat(20_000);
		vi.mocked(encodeGzipBase64).mockResolvedValue("compact-request");
		vi.mocked(sendRequestWithResponse).mockResolvedValue({
			coneMode: false,
			enabled: true,
			patternCount: 20_000,
			patternText: "path",
			patternTextGzipBase64: "",
		});
		const { result } = renderHook(() => useSparseCheckoutManager("repo"));

		await act(() => result.current.run("Set", false, patternText));

		expect(encodeGzipBase64).toHaveBeenCalledWith(patternText);
		expect(sendRequestWithResponse).toHaveBeenCalledWith(
			expect.objectContaining({
				arguments: expect.objectContaining({
					patternText: "",
					patternTextGzipBase64: "compact-request",
				}),
			}),
			{ timeoutMs: 120_000 },
		);
	});
});
