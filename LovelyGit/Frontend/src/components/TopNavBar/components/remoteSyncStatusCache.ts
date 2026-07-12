import type { RemoteSyncStatusResponse } from "@/generated/types";

const MAX_CACHED_REPOSITORIES = 4;
const entries = new Map<string, RemoteSyncStatusResponse>();

export function getCachedRemoteSyncStatus(repositoryId: string) {
	const status = entries.get(repositoryId);
	if (!status) return null;
	entries.delete(repositoryId);
	entries.set(repositoryId, status);
	return status;
}

export function setCachedRemoteSyncStatus(
	repositoryId: string,
	status: RemoteSyncStatusResponse,
) {
	entries.delete(repositoryId);
	entries.set(repositoryId, status);
	while (entries.size > MAX_CACHED_REPOSITORIES) {
		const oldest = entries.keys().next().value;
		if (oldest === undefined) break;
		entries.delete(oldest);
	}
}

export function clearRemoteSyncStatusCache() {
	entries.clear();
}
