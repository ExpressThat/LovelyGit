// @vitest-environment jsdom

import { renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useWorkingTreeChanges } from "./useWorkingTreeChanges";
import {
	loadWorkingTreeChangeSummary,
	loadWorkingTreeChanges,
} from "./WorkingTreeChangesRequests";
import {
	clearWorkingTreeChangesCache,
	setCachedWorkingTreeChanges,
} from "./workingTreeChangesCache";
import {
	clearWorkingTreeSummaryCache,
	setCachedWorkingTreeSummary,
} from "./workingTreeSummaryCache";

vi.mock("@/lib/commands", () => ({
	subscribeToServerEvent: vi.fn(() => vi.fn()),
}));
vi.mock("./WorkingTreeChangesRequests", () => ({
	loadWorkingTreeChangeSummary: vi.fn(),
	loadWorkingTreeChanges: vi.fn(),
}));

describe("useWorkingTreeChanges cache restoration", () => {
	beforeEach(() => {
		clearWorkingTreeSummaryCache();
		clearWorkingTreeChangesCache();
		vi.clearAllMocks();
	});

	it("restores a bounded result without rescanning after switching tabs", () => {
		setCachedWorkingTreeSummary("repo", {
			hasChanges: true,
			isComplete: true,
			shouldPreloadChanges: true,
			totalCount: 2,
		});
		setCachedWorkingTreeChanges("repo", {
			isComplete: true,
			staged: [],
			unstaged: [],
			untracked: [],
			unmerged: [],
			totalCount: 2,
		});

		const { result } = renderHook(() => useWorkingTreeChanges("repo", false));

		expect(result.current.status).toBe("loaded");
		expect(result.current.totalCount).toBe(2);
		expect(loadWorkingTreeChanges).not.toHaveBeenCalled();
		expect(loadWorkingTreeChangeSummary).not.toHaveBeenCalled();
	});
});
