// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import { useBranchMutationRunner } from "./useBranchMutationRunner";

const send = vi.hoisted(() => vi.fn(async () => undefined));
const waitForPaint = vi.hoisted(() => vi.fn<() => Promise<void>>());

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: send }));
vi.mock("@/lib/waitForBrowserPaint", () => ({
	waitForBrowserPaint: waitForPaint,
}));
vi.mock("sonner", () => ({
	toast: {
		error: vi.fn(),
		loading: vi.fn(() => "toast-1"),
		success: vi.fn(),
	},
}));

it("paints branch controls as busy before entering the native bridge", async () => {
	let finishPaint: (() => void) | undefined;
	waitForPaint.mockReturnValueOnce(
		new Promise<void>((resolve) => {
			finishPaint = resolve;
		}),
	);
	const { result } = renderHook(() => useBranchMutationRunner("repo-1"));

	let operation: Promise<void> | undefined;
	act(() => {
		operation = result.current.mutate("feature", "Switching", "Switched", {
			commandType: "CheckoutBranch",
		});
	});
	expect(result.current.busyBranch).toBe("feature");
	expect(send).not.toHaveBeenCalled();

	finishPaint?.();
	await act(async () => operation);
	expect(send).toHaveBeenCalledOnce();
	expect(result.current.busyBranch).toBeNull();
});
