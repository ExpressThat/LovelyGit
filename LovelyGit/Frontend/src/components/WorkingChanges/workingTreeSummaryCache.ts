import type { WorkingTreeChangeSummaryResponse } from "@/generated/types";
import { MAX_CACHED_REPOSITORIES } from "@/lib/repositoryCacheLimits";

const entries = new Map<string, WorkingTreeChangeSummaryResponse>();

export function getCachedWorkingTreeSummary(repositoryId: string) {
	const summary = entries.get(repositoryId);
	if (!summary) return null;
	entries.delete(repositoryId);
	entries.set(repositoryId, summary);
	return summary;
}

export function setCachedWorkingTreeSummary(
	repositoryId: string,
	summary: WorkingTreeChangeSummaryResponse,
) {
	entries.delete(repositoryId);
	entries.set(repositoryId, summary);
	while (entries.size > MAX_CACHED_REPOSITORIES) {
		const oldest = entries.keys().next().value;
		if (oldest === undefined) break;
		entries.delete(oldest);
	}
}

export function invalidateWorkingTreeSummary(repositoryId: string) {
	entries.delete(repositoryId);
}

export function clearWorkingTreeSummaryCache() {
	entries.clear();
}

export function cacheCompleteWorkingTreeSummary(
	repositoryId: string,
	totalCount: number,
) {
	setCachedWorkingTreeSummary(repositoryId, {
		hasChanges: totalCount > 0,
		isComplete: true,
		shouldPreloadChanges: true,
		totalCount,
	});
}
