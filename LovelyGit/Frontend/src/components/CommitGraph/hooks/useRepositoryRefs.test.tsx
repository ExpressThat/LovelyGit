// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { RepositoryRefsResponse } from "@/generated/types";
import { subscribeToServerEvent } from "@/lib/commands";
import {
	getCachedRepositoryRefs,
	loadRepositoryRefs,
} from "@/lib/repositoryRefsCache";
import {
	CACHED_REFS_REFRESH_DELAY_MS,
	useRepositoryRefs,
} from "./useRepositoryRefs";

vi.mock("@/lib/commands", () => ({
	subscribeToServerEvent: vi.fn(() => vi.fn()),
}));
vi.mock("@/lib/repositoryRefsCache", () => ({
	getCachedRepositoryRefs: vi.fn(),
	loadRepositoryRefs: vi.fn(),
	setCachedRepositoryRefs: vi.fn(),
}));

const getCached = vi.mocked(getCachedRepositoryRefs);
const load = vi.mocked(loadRepositoryRefs);
const subscribe = vi.mocked(subscribeToServerEvent);

describe("useRepositoryRefs", () => {
	beforeEach(() => {
		vi.useFakeTimers();
		getCached.mockReset();
		load.mockReset();
		subscribe.mockClear();
	});

	afterEach(() => vi.useRealTimers());

	it("loads uncached refs immediately", async () => {
		getCached.mockReturnValue(null);
		load.mockResolvedValue(refs("main"));
		const { result } = renderHook(() => useRepositoryRefs("repo", 0));

		expect(load).toHaveBeenCalledWith("repo", true);
		await flushPromises();
		expect(result.current.status).toBe("loaded");
		expect(result.current.refs?.currentBranchName).toBe("main");
	});

	it("shows cached refs immediately and defers revalidation", async () => {
		getCached.mockReturnValue(refs("cached"));
		load.mockResolvedValue(refs("fresh"));
		const { result } = renderHook(() => useRepositoryRefs("repo", 0));

		expect(result.current.refs?.currentBranchName).toBe("cached");
		expect(load).not.toHaveBeenCalled();
		act(() => vi.advanceTimersByTime(CACHED_REFS_REFRESH_DELAY_MS - 1));
		expect(load).not.toHaveBeenCalled();
		act(() => vi.advanceTimersByTime(1));
		await flushPromises();

		expect(load).toHaveBeenCalledWith("repo", true);
		expect(result.current.refs?.currentBranchName).toBe("fresh");
	});

	it("does not start an expensive refs read for an abandoned tab", () => {
		getCached.mockImplementation((repositoryId) => refs(repositoryId));
		load.mockResolvedValue(refs("fresh"));
		const { rerender } = renderHook(
			({ repositoryId }) => useRepositoryRefs(repositoryId, 0),
			{ initialProps: { repositoryId: "repo-a" } },
		);

		rerender({ repositoryId: "repo-b" });
		act(() => vi.advanceTimersByTime(CACHED_REFS_REFRESH_DELAY_MS - 1));
		expect(load).not.toHaveBeenCalled();
		act(() => vi.advanceTimersByTime(CACHED_REFS_REFRESH_DELAY_MS));

		expect(load).toHaveBeenCalledOnce();
		expect(load).toHaveBeenCalledWith("repo-b", true);
	});

	it("refreshes immediately after a graph invalidation", () => {
		let graphChanged: (() => void) | undefined;
		subscribe.mockImplementation((_event, listener) => {
			graphChanged = listener as () => void;
			return vi.fn();
		});
		getCached.mockReturnValue(refs("cached"));
		load.mockResolvedValue(refs("fresh"));
		renderHook(() => useRepositoryRefs("repo", 0));

		act(() => graphChanged?.());

		expect(load).toHaveBeenCalledOnce();
		expect(load).toHaveBeenCalledWith("repo", true);
		act(() => vi.advanceTimersByTime(CACHED_REFS_REFRESH_DELAY_MS));
		expect(load).toHaveBeenCalledOnce();
	});

	it("can retry after a failed read", async () => {
		getCached.mockReturnValue(null);
		load
			.mockRejectedValueOnce(new Error("refs failed"))
			.mockResolvedValueOnce(refs("retry"));
		const { result } = renderHook(() => useRepositoryRefs("repo", 0));
		await flushPromises();
		expect(result.current.status).toBe("error");

		act(() => result.current.refresh());
		await flushPromises();

		expect(result.current.status).toBe("loaded");
		expect(result.current.refs?.currentBranchName).toBe("retry");
	});
});

async function flushPromises() {
	await act(async () => Promise.resolve());
}

function refs(branch: string): RepositoryRefsResponse {
	return {
		branchUpstreams: [],
		currentBranchName: branch,
		refs: [],
		remotePrefixes: [],
		stashes: [],
		worktrees: [],
	};
}
