// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, expect, it, vi } from "vitest";
import { useInteractiveRebasePlan } from "./useInteractiveRebasePlan";

const send = vi.hoisted(() => vi.fn());
const waitForPaint = vi.hoisted(() => vi.fn<() => Promise<void>>());

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: send }));
vi.mock("@/lib/waitForBrowserPaint", () => ({
	waitForBrowserPaint: waitForPaint,
}));

beforeEach(() => {
	send.mockReset();
	waitForPaint.mockReset();
	send.mockResolvedValueOnce({
		baseCommitHash: "base",
		commits: [
			{
				authorName: "LovelyGit",
				authorUnixSeconds: 1,
				hash: "commit-1",
				subject: "First commit",
			},
		],
		currentBranchName: "main",
	});
});

it("paints the disabled state before entering the native bridge", async () => {
	let finishPaint: (() => void) | undefined;
	waitForPaint.mockReturnValueOnce(
		new Promise<void>((resolve) => {
			finishPaint = resolve;
		}),
	);
	send.mockResolvedValueOnce({ isCompleted: true });
	const { result } = renderHook(() =>
		useInteractiveRebasePlan("repo-1", "base"),
	);
	await waitFor(() => expect(result.current.plan).toHaveLength(1));

	let operation: ReturnType<typeof result.current.start> | undefined;
	act(() => {
		operation = result.current.start();
	});
	expect(result.current.isRunning).toBe(true);
	expect(send).toHaveBeenCalledOnce();

	finishPaint?.();
	await act(async () =>
		expect(operation).resolves.toEqual({ isCompleted: true }),
	);
	expect(send).toHaveBeenCalledTimes(2);
	expect(result.current.isRunning).toBe(false);
});

it("re-enables the action after the native request fails", async () => {
	waitForPaint.mockResolvedValue(undefined);
	send.mockRejectedValueOnce(new Error("rebase failed"));
	const { result } = renderHook(() =>
		useInteractiveRebasePlan("repo-1", "base"),
	);
	await waitFor(() => expect(result.current.plan).toHaveLength(1));

	await act(async () =>
		expect(result.current.start()).rejects.toThrow("rebase failed"),
	);
	expect(result.current.isRunning).toBe(false);
});
