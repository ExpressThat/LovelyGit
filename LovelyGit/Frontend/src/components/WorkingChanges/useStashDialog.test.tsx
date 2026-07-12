// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { toast } from "sonner";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { type RepositoryStashItem, StashAction } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { clearRepositoryRefsCache } from "@/lib/repositoryRefsCache";
import { useStashDialog } from "./useStashDialog";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({
	toast: {
		error: vi.fn(),
		loading: vi.fn(() => "stash-toast"),
		success: vi.fn(),
	},
}));

const send = vi.mocked(sendRequestWithResponse);
const stash: RepositoryStashItem = {
	commitHash: "0123456789abcdef",
	createdAtUnixSeconds: 1_700_000_000,
	message: "WIP on main",
	selector: "stash@{0}",
};

describe("useStashDialog", () => {
	beforeEach(() => {
		clearRepositoryRefsCache();
		vi.clearAllMocks();
	});

	it("honors controlled open state and delegates close requests", async () => {
		send.mockResolvedValueOnce({ stashes: [] });
		const onOpenChange = vi.fn();
		const { result } = renderHook(() =>
			useStashDialog("repo", vi.fn(), { onOpenChange, open: true }),
		);
		await waitFor(() => expect(send).toHaveBeenCalledOnce());
		act(() => result.current.setOpen(false));
		expect(onOpenChange).toHaveBeenCalledWith(false);
		expect(result.current.open).toBe(true);
	});

	it("exposes load failure, clears loading, and permits retry", async () => {
		send.mockRejectedValueOnce(new Error("Repository unavailable"));
		const { result } = renderHook(() => useStashDialog("repo", vi.fn()));

		act(() => result.current.setOpen(true));
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.loadError).toBe("Repository unavailable");
		expect(result.current.stashes).toEqual([]);

		act(() => result.current.setOpen(false));
		send.mockResolvedValueOnce({ stashes: [stash] });
		act(() => result.current.setOpen(true));
		await waitFor(() => expect(result.current.stashes).toEqual([stash]));
		expect(result.current.loadError).toBeNull();
	});

	it("preserves a failed destructive target and clears it after retry", async () => {
		const onRepositoryChanged = vi.fn();
		send.mockResolvedValueOnce({ stashes: [stash] });
		const { result } = renderHook(() =>
			useStashDialog("repo", onRepositoryChanged),
		);
		act(() => result.current.setOpen(true));
		await waitFor(() => expect(result.current.stashes).toEqual([stash]));
		act(() => result.current.setDropTarget(stash));

		send.mockRejectedValueOnce(new Error("Stash is locked"));
		await act(() => result.current.runAction(StashAction.Drop, stash));

		expect(result.current.busyAction).toBeNull();
		expect(result.current.dropTarget).toEqual(stash);
		expect(onRepositoryChanged).not.toHaveBeenCalled();
		expect(toast.error).toHaveBeenCalledWith("Stash is locked", {
			id: "stash-toast",
		});

		send.mockResolvedValueOnce(undefined);
		await act(() => result.current.runAction(StashAction.Drop, stash));

		expect(result.current.dropTarget).toBeNull();
		expect(result.current.stashes).toEqual([]);
		expect(onRepositoryChanged).toHaveBeenCalledOnce();
		expect(toast.success).toHaveBeenCalledWith("Delete stash complete", {
			id: "stash-toast",
		});
	});

	it("keeps a failed create message and clears it only after success", async () => {
		const onRepositoryChanged = vi.fn();
		send.mockResolvedValueOnce({ stashes: [] });
		const { result } = renderHook(() =>
			useStashDialog("repo", onRepositoryChanged),
		);
		act(() => {
			result.current.setOpen(true);
			result.current.setMessage("checkpoint");
		});
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		send.mockRejectedValueOnce(new Error("Nothing to save"));
		await act(() => result.current.runAction(StashAction.Create));
		expect(result.current.message).toBe("checkpoint");
		expect(onRepositoryChanged).not.toHaveBeenCalled();

		send.mockResolvedValueOnce(undefined);
		send.mockResolvedValueOnce({ stashes: [stash] });
		await act(() => result.current.runAction(StashAction.Create));
		expect(result.current.message).toBe("");
		expect(result.current.stashes).toEqual([stash]);
		expect(onRepositoryChanged).toHaveBeenCalledOnce();
	});

	it("preserves a failed branch target and clears it after retry", async () => {
		const onRepositoryChanged = vi.fn();
		send.mockResolvedValueOnce({
			refs: [
				{ commitHash: "abc", kind: "Local", name: "main", remoteUrl: null },
			],
			stashes: [stash],
		});
		const { result } = renderHook(() =>
			useStashDialog("repo", onRepositoryChanged),
		);
		act(() => result.current.setOpen(true));
		await waitFor(() => expect(result.current.stashes).toEqual([stash]));
		act(() => result.current.setBranchTarget(stash));

		send.mockRejectedValueOnce(new Error("Branch already exists"));
		await act(() =>
			result.current.runAction(StashAction.Branch, stash, "recover/work"),
		);
		expect(result.current.branchTarget).toEqual(stash);
		expect(onRepositoryChanged).not.toHaveBeenCalled();
		expect(send).toHaveBeenLastCalledWith(
			expect.objectContaining({
				arguments: expect.objectContaining({ branchName: "recover/work" }),
			}),
			expect.any(Object),
		);

		send.mockResolvedValueOnce(undefined);
		await act(() =>
			result.current.runAction(StashAction.Branch, stash, "recover/work"),
		);
		expect(result.current.branchTarget).toBeNull();
		expect(onRepositoryChanged).toHaveBeenCalledOnce();
	});
});
