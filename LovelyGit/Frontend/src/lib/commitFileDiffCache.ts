import type { CommitFileDiffResponse } from "@/generated/types";
import {
	getCompactDiffPayloadIdentity,
	shareCompactDiffPayloadIdentity,
} from "./compactDiffPayloadIdentity";

const MAX_ENTRIES = 8;
const MAX_CACHED_LINE_COUNT = 5_000;
const MAX_CACHED_PAYLOAD_CHARACTERS = 2_000_000;
const cache = new Map<string, CommitFileDiffResponse>();
const oversizedKeys = new Set<string>();
let oversizedIdentity: object | null = null;

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
	deleteCacheEntry(key);
	trackOversizedEntry(key, response);
	cache.set(key, response);
	while (cache.size > MAX_ENTRIES) {
		const oldest = cache.keys().next().value;
		if (!oldest) break;
		deleteCacheEntry(oldest);
	}
}

export function cacheCommitFileDiffViews(
	key: string,
	alternateViewKey: string,
	response: CommitFileDiffResponse,
) {
	cacheCommitFileDiff(key, response);
	if (
		response.compactLineSchema !== "tuple-v4-delta-refs:gzip-base64:utf-8" ||
		!response.compactSourceBundleGzipBase64
	) {
		return;
	}
	const alternateView: CommitFileDiffResponse = {
		...response,
		viewMode: response.viewMode === "Combined" ? "SideBySide" : "Combined",
	};
	shareCompactDiffPayloadIdentity(response, alternateView);
	cacheCommitFileDiff(alternateViewKey, alternateView);
}

export function clearCommitFileDiffCache() {
	cache.clear();
	oversizedKeys.clear();
	oversizedIdentity = null;
}

function trackOversizedEntry(key: string, response: CommitFileDiffResponse) {
	if (!isOversized(response)) return;
	const identity = getCompactDiffPayloadIdentity(response);
	if (oversizedIdentity && oversizedIdentity !== identity) {
		for (const oversizedKey of oversizedKeys) cache.delete(oversizedKey);
		oversizedKeys.clear();
	}
	oversizedIdentity = identity;
	oversizedKeys.add(key);
}

function deleteCacheEntry(key: string) {
	cache.delete(key);
	if (!oversizedKeys.delete(key)) return;
	if (oversizedKeys.size === 0) oversizedIdentity = null;
}

function isOversized(response: CommitFileDiffResponse) {
	const lineCount = Math.max(
		response.lines?.length ?? 0,
		response.compactLineCount ?? 0,
		response.virtualLineCount ?? 0,
	);
	const payloadCharacters =
		(response.compactLinesGzipBase64?.length ?? 0) +
		(response.compactSourceBundleGzipBase64?.length ?? 0) +
		(response.virtualTextGzipBase64?.length ?? 0);
	return (
		lineCount > MAX_CACHED_LINE_COUNT ||
		payloadCharacters > MAX_CACHED_PAYLOAD_CHARACTERS
	);
}
