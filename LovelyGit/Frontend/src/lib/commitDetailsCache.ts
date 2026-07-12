import type { CommitDetailsResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";

const MAX_CACHED_DETAILS = 8;
const MAX_CACHED_CHANGED_FILES = 100;
const entries = new Map<string, CacheEntry>();
let oversizedKey: string | null = null;

export function loadCommitDetails(
	repositoryId: string,
	commitHash: string,
	parentIndex: number,
) {
	const key = cacheKey(repositoryId, commitHash, parentIndex);
	const cached = entries.get(key);
	if (cached?.response) return Promise.resolve(cached.response);
	if (cached?.pending) return cached.pending;

	const entry: CacheEntry = {};
	const pending = sendRequestWithResponse({
		commandType: "GetCommitDetails",
		arguments: { commitHash, parentIndex, repositoryId },
	})
		.then((response) => {
			if (entries.get(key) !== entry) return response;
			if (response.changedFiles.length > MAX_CACHED_CHANGED_FILES) {
				if (oversizedKey && oversizedKey !== key) deleteEntry(oversizedKey);
				oversizedKey = key;
			}
			setEntry(key, { response });
			return response;
		})
		.catch((error: unknown) => {
			if (entries.get(key) === entry) entries.delete(key);
			throw error;
		});
	entry.pending = pending;
	setEntry(key, entry);
	return pending;
}

export async function prefetchCommitDetails(
	repositoryId: string,
	commitHash: string,
) {
	try {
		await loadCommitDetails(repositoryId, commitHash, 0);
	} catch {
		// Selection surfaces the actionable error if the user opens this commit.
	}
}

export function clearCommitDetailsCache() {
	entries.clear();
	oversizedKey = null;
}

function setEntry(key: string, entry: CacheEntry) {
	entries.delete(key);
	entries.set(key, entry);
	while (entries.size > MAX_CACHED_DETAILS) {
		const oldest = entries.keys().next().value;
		if (oldest === undefined) break;
		deleteEntry(oldest);
	}
}

function deleteEntry(key: string) {
	entries.delete(key);
	if (oversizedKey === key) oversizedKey = null;
}

function cacheKey(
	repositoryId: string,
	commitHash: string,
	parentIndex: number,
) {
	return `${repositoryId}:${commitHash}:${parentIndex}`;
}

type CacheEntry = {
	pending?: Promise<CommitDetailsResponse>;
	response?: CommitDetailsResponse;
};
