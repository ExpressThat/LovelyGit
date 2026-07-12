import { beforeEach, describe, expect, it } from "vitest";
import type { RemoteSyncStatusResponse } from "@/generated/types";
import {
	clearRemoteSyncStatusCache,
	getCachedRemoteSyncStatus,
	setCachedRemoteSyncStatus,
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
		for (const name of ["a", "b", "c", "d"]) {
			setCachedRemoteSyncStatus(name, status(name));
		}
		expect(getCachedRemoteSyncStatus("a")?.branchName).toBe("a");
		setCachedRemoteSyncStatus("e", status("e"));

		expect(getCachedRemoteSyncStatus("b")).toBeNull();
		expect(getCachedRemoteSyncStatus("a")?.branchName).toBe("a");
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
