import type { WorkingTreeChangesResponse } from "@/generated/types";
import { MAX_CACHED_REPOSITORIES } from "@/lib/repositoryCacheLimits";

export const MAX_CACHED_WORKING_TREE_FILES_PER_REPOSITORY = 500;
const MAX_CACHED_WORKING_TREE_FILES = 1_000;
const entries = new Map<string, WorkingTreeChangesResponse>();
let cachedFileCount = 0;

export function getCachedWorkingTreeChanges(repositoryId: string) {
	const changes = entries.get(repositoryId);
	if (!changes) return null;
	entries.delete(repositoryId);
	entries.set(repositoryId, changes);
	return changes;
}

export function setCachedWorkingTreeChanges(
	repositoryId: string,
	changes: WorkingTreeChangesResponse,
) {
	invalidateCachedWorkingTreeChanges(repositoryId);
	if (changes.totalCount > MAX_CACHED_WORKING_TREE_FILES_PER_REPOSITORY) {
		return false;
	}

	while (
		entries.size >= MAX_CACHED_REPOSITORIES ||
		cachedFileCount + changes.totalCount > MAX_CACHED_WORKING_TREE_FILES
	) {
		const oldest = entries.keys().next().value;
		if (oldest === undefined) break;
		invalidateCachedWorkingTreeChanges(oldest);
	}
	entries.set(repositoryId, changes);
	cachedFileCount += changes.totalCount;
	return true;
}

export function invalidateCachedWorkingTreeChanges(repositoryId: string) {
	const changes = entries.get(repositoryId);
	if (!changes) return;
	entries.delete(repositoryId);
	cachedFileCount -= changes.totalCount;
}

export function clearWorkingTreeChangesCache() {
	entries.clear();
	cachedFileCount = 0;
}
