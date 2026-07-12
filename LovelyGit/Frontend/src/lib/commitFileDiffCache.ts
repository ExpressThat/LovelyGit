import type { CommitFileDiffResponse } from "@/generated/types";

const MAX_ENTRIES = 8;
const cache = new Map<string, CommitFileDiffResponse>();

export function commitFileDiffCacheKey(input: {
	commitHash: string;
	comparisonCommitHash?: string | null;
	filePath: string;
	ignoreWhitespace: boolean;
	parentIndex: number;
	repositoryId: string;
	viewMode: string;
}) {
	return [
		input.repositoryId,
		input.commitHash,
		input.comparisonCommitHash ?? "",
		input.parentIndex,
		input.filePath,
		input.viewMode,
		input.ignoreWhitespace ? "ignore" : "exact",
	].join("\0");
}

export function getCachedCommitFileDiff(key: string) {
	const response = cache.get(key);
	if (!response) return undefined;
	cache.delete(key);
	cache.set(key, response);
	return response;
}

export function cacheCommitFileDiff(
	key: string,
	response: CommitFileDiffResponse,
) {
	cache.delete(key);
	cache.set(key, response);
	while (cache.size > MAX_ENTRIES) {
		const oldest = cache.keys().next().value;
		if (!oldest) break;
		cache.delete(oldest);
	}
}

export function clearCommitFileDiffCache() {
	cache.clear();
}
