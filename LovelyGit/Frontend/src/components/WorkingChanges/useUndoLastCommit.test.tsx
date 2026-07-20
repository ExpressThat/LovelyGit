// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { HeadCommitMessageResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { useUndoLastCommit } from "./useUndoLastCommit";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({ toast: { success: vi.fn() } }));

const send = vi.mocked(sendRequestWithResponse);

describe("useUndoLastCommit", () => {
	beforeEach(() => send.mockReset());

	it("previews HEAD, undoes the confirmed hash, and restores its message", async () => {
		const message = head();
		const onSuccess = vi.fn();
		send.mockResolvedValueOnce(message).mockResolvedValueOnce(message);
		const { result } = renderUndo(onSuccess);

		await act(() => result.current.open());
		expect(result.current.preview).toEqual(message);
		await act(() => result.current.confirm());

		expect(send).toHaveBeenLastCalledWith(
			{
				commandType: "UndoLastCommit",
				arguments: { expectedHeadHash: message.hash, repositoryId: "repo" },
			},
			{ timeoutMs: gitMutationTimeoutMs },
		);
		expect(onSuccess).toHaveBeenCalledWith(message);
		expect(result.current.isOpen).toBe(false);
	});

	it("does not send a mutation for the initial commit", async () => {
		send.mockResolvedValueOnce(head({ firstParentHash: null, parentCount: 0 }));
		const { result } = renderUndo(vi.fn());

		await act(() => result.current.open());
		await act(() => result.current.confirm());

		expect(send).toHaveBeenCalledOnce();
		expect(result.current.isOpen).toBe(true);
	});

	it("closes after Git succeeds while reconciliation remains protected", async () => {
		const message = head();
		let finishRefresh: (() => void) | undefined;
		const onSuccess = vi.fn(
			() => new Promise<void>((resolve) => (finishRefresh = resolve)),
		);
		send.mockResolvedValueOnce(message).mockResolvedValueOnce(message);
		const { result } = renderUndo(onSuccess);
		await act(() => result.current.open());

		let confirmation: Promise<void> | undefined;
		act(() => {
			confirmation = result.current.confirm();
		});
		await waitFor(() => expect(onSuccess).toHaveBeenCalledWith(message));
		expect(result.current.isOpen).toBe(false);
		expect(result.current.isUndoing).toBe(true);

		await act(async () => finishRefresh?.());
		await act(async () => confirmation);
		expect(result.current.isUndoing).toBe(false);
	});

	it("does not turn a completed undo into a refresh failure", async () => {
		const message = head();
		send.mockResolvedValueOnce(message).mockResolvedValueOnce(message);
		const { result } = renderUndo(() => Promise.reject(new Error("refresh")));

		await act(() => result.current.open());
		await act(() => result.current.confirm());

		expect(result.current.isOpen).toBe(false);
		expect(result.current.error).toBeNull();
		expect(result.current.isBusy).toBe(false);
	});

	it("keeps the dialog open, re-enables it, and permits a successful retry", async () => {
		const message = head();
		const onSuccess = vi.fn();
		send
			.mockResolvedValueOnce(message)
			.mockRejectedValueOnce(new Error("index is locked"))
			.mockResolvedValueOnce(message);
		const { result } = renderUndo(onSuccess);

		await act(() => result.current.open());
		await act(() => result.current.confirm());
		expect(result.current.error).toBe("index is locked");
		expect(result.current.isOpen).toBe(true);
		expect(result.current.isBusy).toBe(false);

		await act(() => result.current.confirm());
		expect(onSuccess).toHaveBeenCalledOnce();
		expect(result.current.isOpen).toBe(false);
	});
});

function renderUndo(
	onSuccess: (message: HeadCommitMessageResponse) => Promise<void> | void,
) {
	return renderHook(() =>
		useUndoLastCommit({ onSuccess, repositoryId: "repo" }),
	);
}

function head(
	overrides: Partial<HeadCommitMessageResponse> = {},
): HeadCommitMessageResponse {
	return {
		body: "Original body",
		firstParentHash: "b".repeat(40),
		hash: "a".repeat(40),
		parentCount: 1,
		title: "Original title",
		...overrides,
	};
}
