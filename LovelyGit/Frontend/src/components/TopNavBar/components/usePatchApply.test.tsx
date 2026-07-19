// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { nativeDialogTimeoutMs } from "@/lib/nativeDialogTimeout";
import { usePatchApply } from "./usePatchApply";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
const waitForPaint = vi.hoisted(() =>
	vi.fn<() => Promise<void>>(async () => undefined),
);
vi.mock("@/lib/waitForBrowserPaint", () => ({
	waitForBrowserPaint: waitForPaint,
}));

describe("usePatchApply", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		waitForPaint.mockResolvedValue(undefined);
	});

	it("opens a native patch preview", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValue(preview);
		const { result } = renderHook(() =>
			usePatchApply("repository-id", vi.fn()),
		);

		await act(() => result.current.choosePatch());

		expect(sendRequestWithResponse).toHaveBeenCalledWith(
			{ commandType: "ChoosePatchFile" },
			{ timeoutMs: nativeDialogTimeoutMs },
		);
		expect(result.current.preview).toEqual(preview);
	});

	it("applies the selected patch with staging and reverse options", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockResolvedValueOnce(preview)
			.mockResolvedValueOnce(undefined);
		const onApplied = vi.fn();
		const { result } = renderHook(() =>
			usePatchApply("repository-id", onApplied),
		);
		await act(() => result.current.choosePatch());
		act(() => {
			result.current.setStageChanges(true);
			result.current.setReverse(true);
		});

		await act(() => result.current.applyPatch());

		expect(sendRequestWithResponse).toHaveBeenLastCalledWith(
			{
				arguments: {
					patchPath: preview.path,
					repositoryId: "repository-id",
					reverse: true,
					stageChanges: true,
				},
				commandType: "ApplyPatch",
			},
			{ timeoutMs: gitMutationTimeoutMs },
		);
		expect(waitForPaint).toHaveBeenCalledOnce();
		expect(result.current.preview).toBeNull();
		expect(onApplied).toHaveBeenCalledOnce();
	});

	it("paints the protected state before entering the native bridge", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockResolvedValueOnce(preview)
			.mockResolvedValueOnce(undefined);
		let finishPaint: (() => void) | undefined;
		waitForPaint.mockReturnValueOnce(
			new Promise<void>((resolve) => {
				finishPaint = resolve;
			}),
		);
		const { result } = renderHook(() =>
			usePatchApply("repository-id", vi.fn()),
		);
		await act(() => result.current.choosePatch());

		let apply: Promise<void> | undefined;
		act(() => {
			apply = result.current.applyPatch();
		});

		expect(result.current.isApplying).toBe(true);
		expect(sendRequestWithResponse).toHaveBeenCalledOnce();
		finishPaint?.();
		await act(() => apply);
		expect(sendRequestWithResponse).toHaveBeenCalledTimes(2);
	});

	it("does nothing without a repository", async () => {
		const { result } = renderHook(() => usePatchApply(null, vi.fn()));

		await act(() => result.current.choosePatch());

		expect(sendRequestWithResponse).not.toHaveBeenCalled();
	});

	it("keeps the preview open when Git preflight rejects the patch", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockResolvedValueOnce(preview)
			.mockRejectedValueOnce(new Error("patch does not apply"));
		const onApplied = vi.fn();
		const { result } = renderHook(() =>
			usePatchApply("repository-id", onApplied),
		);
		await act(() => result.current.choosePatch());

		await act(() => result.current.applyPatch());

		expect(result.current.preview).toEqual(preview);
		expect(onApplied).not.toHaveBeenCalled();
		expect(result.current.isApplying).toBe(false);
	});

	it("does not open a preview when file selection is cancelled", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValue({
			fileName: null,
			files: [],
			isTruncated: false,
			path: null,
			selected: false,
			totalAdditions: 0,
			totalDeletions: 0,
		});
		const { result } = renderHook(() =>
			usePatchApply("repository-id", vi.fn()),
		);

		await act(() => result.current.choosePatch());

		expect(result.current.preview).toBeNull();
	});
});

const preview = {
	fileName: "change.patch",
	files: [{ additions: 2, deletions: 1, path: "src/file.ts" }],
	isTruncated: false,
	path: "C:/patches/change.patch",
	selected: true,
	totalAdditions: 2,
	totalDeletions: 1,
};
