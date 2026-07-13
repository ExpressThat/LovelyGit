import type {
	CommitSearchResponse,
	CommitSearchResult,
} from "@/generated/types";

export function searchResult(
	overrides: Partial<CommitSearchResult> = {},
): CommitSearchResult {
	return {
		author: "Ross",
		date: 1_700_000_000,
		email: "ross@example.invalid",
		hash: "abcdef1234567890abcdef1234567890abcdef12",
		preview: "Needle result",
		refs: ["main"],
		subject: "Needle result",
		...overrides,
	};
}

export function searchResponse(
	results: CommitSearchResult[] = [searchResult()],
): CommitSearchResponse {
	return {
		afterUnixSeconds: null,
		author: "",
		beforeUnixSeconds: null,
		isPartial: false,
		matchingCommitCount: results.length,
		query: "needle",
		results,
		scannedCommitCount: 42,
	};
}
