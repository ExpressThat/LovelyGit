import type { RepositoryRefsResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";
import {
	MAX_CACHED_REFERENCE_ITEMS,
	MAX_CACHED_REPOSITORIES,
} from "@/lib/repositoryCacheLimits";
import { expandRepositoryRefsPayload } from "@/lib/repositoryRefsPayload";

const entries = new Map<string, CacheEntry>();
const listeners = new Map<string, Set<() => void>>();

export function loadRepositoryRefs(
	repositoryId: string,
	forceRefresh = false,
): Promise<RepositoryRefsResponse> {
	const cached = entries.get(repositoryId);
	if (!forceRefresh) {
		if (cached?.response) return Promise.resolve(cached.response);
		if (cached?.pending) return cached.pending;
	}

	const entry: CacheEntry = cached?.response
		? { response: cached.response, weight: cached.weight }
		: {};
	const pending = sendRequestWithResponse({
		arguments: { knownRepositoryId: repositoryId },
		commandType: NativeMessageType.GetRepositoryRefs,
	})
		.then(expandRepositoryRefsPayload)
		.then((response) => {
			if (entries.get(repositoryId) === entry) {
				setEntry(repositoryId, { response });
			}
			return response;
		})
		.catch((error: unknown) => {
			if (entries.get(repositoryId) === entry) {
				if (entry.response) {
					setEntry(repositoryId, {
						response: entry.response,
						weight: entry.weight,
					});
				} else {
					entries.delete(repositoryId);
				}
			}
			throw error;
		});
	entry.pending = pending;
	setEntry(repositoryId, entry);
	return pending;
}

export function setCachedRepositoryRefs(
	repositoryId: string,
	response: RepositoryRefsResponse,
) {
	setEntry(repositoryId, { response });
}

export function getCachedRepositoryRefs(repositoryId: string) {
	const entry = entries.get(repositoryId);
	const response = entry?.response;
	if (response && entry) setEntry(repositoryId, entry);
	return response ?? null;
}

export function clearRepositoryRefsCache() {
	entries.clear();
	for (const repositoryListeners of listeners.values()) {
		for (const listener of repositoryListeners) listener();
	}
}

export function invalidateRepositoryRefs(repositoryId: string) {
	entries.delete(repositoryId);
	notify(repositoryId);
}

export function subscribeRepositoryRefs(
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

function setEntry(repositoryId: string, entry: CacheEntry) {
	const previousResponse = entries.get(repositoryId)?.response;
	if (entry.response && entry.weight === undefined) {
		entry.weight = referenceItemCount(entry.response);
	}
	entries.delete(repositoryId);
	entries.set(repositoryId, entry);
	while (
		entries.size > MAX_CACHED_REPOSITORIES ||
		(entries.size > 1 &&
			cachedReferenceItemCount() > MAX_CACHED_REFERENCE_ITEMS)
	) {
		const oldest = entries.keys().next().value;
		if (oldest === undefined) break;
		entries.delete(oldest);
	}
	if (entry.response !== previousResponse) notify(repositoryId);
}

function notify(repositoryId: string) {
	const repositoryListeners = listeners.get(repositoryId);
	if (!repositoryListeners) return;
	for (const listener of repositoryListeners) listener();
}

function cachedReferenceItemCount() {
	let count = 0;
	for (const entry of entries.values()) {
		count += entry.weight ?? 0;
	}
	return count;
}

function referenceItemCount(response: RepositoryRefsResponse) {
	return (
		(response.refs?.length ?? 0) +
		(response.worktrees?.length ?? 0) +
		(response.stashes?.length ?? 0) +
		(response.branchUpstreams?.length ?? 0) +
		(response.remotePrefixes?.length ?? 0)
	);
}

type CacheEntry = {
	pending?: Promise<RepositoryRefsResponse>;
	response?: RepositoryRefsResponse;
	weight?: number;
};
