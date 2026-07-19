import { beforeEach, describe, expect, it } from "vitest";
import type { RemoteSyncStatusResponse } from "@/generated/types";
import {
	clearRemoteSyncStatusCache,
	getCachedRemoteSyncStatus,
	invalidateRemoteSyncStatus,
	setCachedRemoteSyncStatus,
	subscribeRemoteSyncStatus,
} from "./remoteSyncStatusCache";

describe("remoteSyncStatusCache", () => {
	beforeEach(clearRemoteSyncStatusCache);

	it("stores status by repository", () => {
		const value = status("main");
		setCachedRemoteSyncStatus("repo", value);

		expect(getCachedRemoteSyncStatus("repo")).toBe(value);
		expect(getCachedRemoteSyncStatus("missing")).toBeNull();
	});

	it("evicts the least recently used repository", () => {
		for (const name of ["a", "b", "c", "d", "e", "f", "g", "h"]) {
			setCachedRemoteSyncStatus(name, status(name));
		}
		expect(getCachedRemoteSyncStatus("a")?.branchName).toBe("a");
		setCachedRemoteSyncStatus("i", status("i"));

		expect(getCachedRemoteSyncStatus("b")).toBeNull();
		expect(getCachedRemoteSyncStatus("a")?.branchName).toBe("a");
	});

	it("invalidates only the requested repository and notifies its subscribers", () => {
		setCachedRemoteSyncStatus("repo", status("main"));
		setCachedRemoteSyncStatus("other", status("other"));
		let notifications = 0;
		const unsubscribe = subscribeRemoteSyncStatus(
			"repo",
			() => notifications++,
		);

		invalidateRemoteSyncStatus("repo");
		unsubscribe();
		invalidateRemoteSyncStatus("repo");

		expect(notifications).toBe(1);
		expect(getCachedRemoteSyncStatus("repo")).toBeNull();
		expect(getCachedRemoteSyncStatus("other")?.branchName).toBe("other");
	});
});

function status(branchName: string): RemoteSyncStatusResponse {
	return {
		aheadCount: 0,
		behindCount: 0,
		branchName,
		hasUpstream: true,
		isHistoryPartial: false,
		isUpstreamAvailable: true,
		localHash: "local",
		upstreamHash: "remote",
		upstreamName: `origin/${branchName}`,
	};
}
