import type { RemoteSyncStatusResponse } from "@/generated/types";
import { MAX_CACHED_REPOSITORIES } from "@/lib/repositoryCacheLimits";

const entries = new Map<string, RemoteSyncStatusResponse>();
const listeners = new Map<string, Set<() => void>>();

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

export function invalidateRemoteSyncStatus(repositoryId: string) {
	entries.delete(repositoryId);
	for (const listener of listeners.get(repositoryId) ?? []) listener();
}

export function subscribeRemoteSyncStatus(
	repositoryId: string,
	listener: () => void,
) {
	let repositoryListeners = listeners.get(repositoryId);
	if (!repositoryListeners) {
		repositoryListeners = new Set();
		listeners.set(repositoryId, repositoryListeners);
	}
	repositoryListeners.add(listener);
	return () => {
		repositoryListeners.delete(listener);
		if (repositoryListeners.size === 0) listeners.delete(repositoryId);
	};
}
