import type { GitBisectState } from "@/generated/types";

const MAX_CACHED_REPOSITORIES = 4;
const entries = new Map<string, GitBisectState>();

export function getCachedBisectState(repositoryId: string) {
	const state = entries.get(repositoryId);
	if (!state) return null;
	entries.delete(repositoryId);
	entries.set(repositoryId, state);
	return state;
}

export function setCachedBisectState(
	repositoryId: string,
	state: GitBisectState,
) {
	entries.delete(repositoryId);
	entries.set(repositoryId, state);
	while (entries.size > MAX_CACHED_REPOSITORIES) {
		const oldest = entries.keys().next().value;
		if (oldest === undefined) break;
		entries.delete(oldest);
	}
}

export function clearBisectStateCache() {
	entries.clear();
}
