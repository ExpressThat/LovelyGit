// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { toast } from "sonner";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { type RepositoryStashItem, StashAction } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { clearRepositoryRefsCache } from "@/lib/repositoryRefsCache";
import { useStashDialog } from "./useStashDialog";
import { waitForWorkingTreePaint } from "./WorkingTreePaintBoundary";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("./WorkingTreePaintBoundary", () => ({
	waitForWorkingTreePaint: vi.fn().mockResolvedValue(undefined),
}));
vi.mock("sonner", () => ({
	toast: {
		error: vi.fn(),
		loading: vi.fn(() => "stash-toast"),
		success: vi.fn(),
	},
}));

const send = vi.mocked(sendRequestWithResponse);
const waitForPaint = vi.mocked(waitForWorkingTreePaint);
const stash: RepositoryStashItem = {
	commitHash: "0123456789abcdef",
	createdAtUnixSeconds: 1_700_000_000,
	message: "WIP on main",
	selector: "stash@{0}",
};

describe("useStashDialog responsiveness", () => {
	beforeEach(() => {
		clearRepositoryRefsCache();
		vi.clearAllMocks();
		waitForPaint.mockResolvedValue(undefined);
	});

	it("paints the busy state before entering the native bridge", async () => {
		let releasePaint!: () => void;
		const paint = new Promise<void>((resolve) => {
			releasePaint = resolve;
		});
		waitForPaint.mockReturnValueOnce(paint);
		send.mockResolvedValueOnce(undefined);
		const { result } = renderHook(() => useStashDialog("repo", vi.fn()));
		let pending!: Promise<void>;

		act(() => {
			pending = result.current.runAction(StashAction.Drop, stash);
		});

		expect(result.current.busyAction).toBe(StashAction.Drop);
		expect(send).not.toHaveBeenCalled();
		await act(async () => {
			releasePaint();
			await pending;
		});
		expect(send).toHaveBeenCalledOnce();
		expect(result.current.busyAction).toBeNull();
	});

	it("refreshes the created stash before publishing the repository change", async () => {
		const order: string[] = [];
		const onRepositoryChanged = vi.fn(() => {
			order.push("repository");
		});
		send
			.mockImplementationOnce(async () => {
				order.push("create");
			})
			.mockImplementationOnce(async () => {
				order.push("stashes");
				return { refs: [], stashes: [stash] };
			});
		const { result } = renderHook(() =>
			useStashDialog("repo", onRepositoryChanged),
		);

		await act(() => result.current.runAction(StashAction.Create));

		expect(order).toEqual(["create", "stashes", "repository"]);
		expect(result.current.stashes).toEqual([stash]);
	});

	it("retries when an in-flight ref snapshot misses the created stash", async () => {
		send
			.mockResolvedValueOnce(undefined)
			.mockResolvedValueOnce({ refs: [], stashes: [] })
			.mockResolvedValueOnce({ refs: [], stashes: [stash] });
		const { result } = renderHook(() => useStashDialog("repo", vi.fn()));

		await act(() => result.current.runAction(StashAction.Create));

		expect(send).toHaveBeenCalledTimes(3);
		expect(result.current.stashes).toEqual([stash]);
	});

	it("does not let the dialog's initial load overwrite a created stash", async () => {
		let releaseInitial!: (value: unknown) => void;
		const initial = new Promise((resolve) => {
			releaseInitial = resolve;
		});
		send
			.mockReturnValueOnce(initial as ReturnType<typeof send>)
			.mockResolvedValueOnce(undefined)
			.mockResolvedValueOnce({ refs: [], stashes: [stash] });
		const { result } = renderHook(() => useStashDialog("repo", vi.fn()));
		act(() => result.current.setOpen(true));
		await waitFor(() => expect(send).toHaveBeenCalledOnce());

		await act(() => result.current.runAction(StashAction.Create));
		expect(result.current.stashes).toEqual([stash]);
		await act(async () => {
			releaseInitial({ refs: [], stashes: [] });
			await initial;
		});

		expect(result.current.stashes).toEqual([stash]);
		expect(result.current.loadError).toBeNull();
	});

	it("publishes create success without waiting for repository reconciliation", async () => {
		const reconciliation = deferred<void>();
		const onRepositoryChanged = vi.fn(() => reconciliation.promise);
		const onCreateSuccess = vi.fn();
		send.mockResolvedValueOnce({ refs: [], stashes: [stash] });
		const { result } = renderHook(() =>
			useStashDialog(
				"repo",
				onRepositoryChanged,
				undefined,
				false,
				[],
				onCreateSuccess,
			),
		);

		await act(() => result.current.runAction(StashAction.Create));

		expect(onCreateSuccess).toHaveBeenCalledWith(false, [], true);
		expect(onRepositoryChanged).toHaveBeenCalledOnce();
		expect(result.current.busyAction).toBeNull();
		expect(toast.success).toHaveBeenCalledWith("Stash changes complete", {
			id: "stash-toast",
		});
		reconciliation.reject(new Error("status refresh failed"));
		await act(async () => Promise.resolve());
		expect(toast.error).not.toHaveBeenCalled();
	});

	it("does not relabel a successful create when reconciliation throws", async () => {
		send.mockResolvedValueOnce({ refs: [], stashes: [stash] });
		const onRepositoryChanged = vi.fn(() => {
			throw new Error("refresh unavailable");
		});
		const { result } = renderHook(() =>
			useStashDialog("repo", onRepositoryChanged),
		);

		await act(() => result.current.runAction(StashAction.Create));

		expect(result.current.busyAction).toBeNull();
		expect(toast.success).toHaveBeenCalledWith("Stash changes complete", {
			id: "stash-toast",
		});
		expect(toast.error).not.toHaveBeenCalled();
	});
});

function deferred<T>() {
	let resolve!: (value: T) => void;
	let reject!: (error: Error) => void;
	const promise = new Promise<T>((complete, fail) => {
		resolve = complete;
		reject = fail;
	});
	return { promise, reject, resolve };
}
