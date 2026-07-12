import type { RepositoryRefsResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";

const MAX_CACHED_REPOSITORIES = 4;
const entries = new Map<string, CacheEntry>();

export function loadRepositoryRefs(
	repositoryId: string,
	forceRefresh = false,
): Promise<RepositoryRefsResponse> {
	if (forceRefresh) entries.delete(repositoryId);
	const cached = entries.get(repositoryId);
	if (cached?.response) return Promise.resolve(cached.response);
	if (cached?.pending) return cached.pending;

	const entry: CacheEntry = {};
	const pending = sendRequestWithResponse({
		arguments: { knownRepositoryId: repositoryId },
		commandType: NativeMessageType.GetRepositoryRefs,
	})
		.then((response) => {
			if (entries.get(repositoryId) === entry) {
				setEntry(repositoryId, { response });
			}
			return response;
		})
		.catch((error: unknown) => {
			if (entries.get(repositoryId) === entry) {
				entries.delete(repositoryId);
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

export function clearRepositoryRefsCache() {
	entries.clear();
}

export function invalidateRepositoryRefs(repositoryId: string) {
	entries.delete(repositoryId);
}

function setEntry(repositoryId: string, entry: CacheEntry) {
	entries.delete(repositoryId);
	entries.set(repositoryId, entry);
	while (entries.size > MAX_CACHED_REPOSITORIES) {
		const oldest = entries.keys().next().value;
		if (oldest === undefined) break;
		entries.delete(oldest);
	}
}

type CacheEntry = {
	pending?: Promise<RepositoryRefsResponse>;
	response?: RepositoryRefsResponse;
};
