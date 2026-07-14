import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	activateCommitGraphSession,
	CACHED_REFRESH_DELAY_MS,
	deferCachedCommitGraphRefresh,
	MAX_CACHED_ROWS,
	resetCommitGraphSessionCacheForTests,
	session,
} from "./commitGraphSession";

describe("commitGraphSession tab cache", () => {
	beforeEach(() => resetCommitGraphSessionCacheForTests());

	it("restores a bounded top-of-graph preview while forcing a fresh load", () => {
		activateCommitGraphSession("first");
		session.currentBranchName = "main";
		session.laneCount = 7;
		session.rows = Array.from({ length: 400 }, () => null);
		session.totalRows = 50_000;
		session.loadedRowCount = 400;
		session.nextCursor = "stale-cursor";
		activateCommitGraphSession("second");

		activateCommitGraphSession("first");

		expect(session.currentBranchName).toBe("main");
		expect(session.laneCount).toBe(7);
		expect(session.rows).toHaveLength(MAX_CACHED_ROWS);
		expect(session.totalRows).toBe(50_000);
		expect(session.loadedRowCount).toBe(0);
		expect(session.nextCursor).toBeNull();
		expect(session.loading).toBe(false);
	});

	it("keeps native refresh work outside the immediate interaction window", () => {
		vi.useFakeTimers();
		activateCommitGraphSession("repo");
		const refresh = vi.fn();

		const cancel = deferCachedCommitGraphRefresh(refresh);
		vi.advanceTimersByTime(CACHED_REFRESH_DELAY_MS - 1);
		expect(refresh).not.toHaveBeenCalled();
		vi.advanceTimersByTime(1);
		expect(refresh).toHaveBeenCalledOnce();

		cancel();
		vi.useRealTimers();
	});

	it("retains eight inactive repository previews", () => {
		for (const [index, repositoryId] of [
			"a",
			"b",
			"c",
			"d",
			"e",
			"f",
			"g",
			"h",
			"i",
		].entries()) {
			activateCommitGraphSession(repositoryId);
			session.rows = [null];
			session.totalRows = index + 1;
		}

		activateCommitGraphSession("a");
		expect(session.totalRows).toBe(0);
		activateCommitGraphSession("b");
		expect(session.totalRows).toBe(2);
	});
});
