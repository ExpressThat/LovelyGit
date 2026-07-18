import { useEffect, useState } from "react";
import type { CommitSearchResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import {
	type CommitSearchFilters,
	emptyCommitSearchFilters,
	hasCommitSearchFilter,
	isCommitSearchDateRangeValid,
	toSearchBoundaries,
} from "./commitSearchFilters";

const SEARCH_DEBOUNCE_MS = 140;
const MINIMUM_QUERY_LENGTH = 2;

export function useCommitSearch(
	repositoryId: string | null,
	query: string,
	enabled: boolean,
	deep = false,
	filters: CommitSearchFilters = emptyCommitSearchFilters,
	scopeIsValid = true,
) {
	const [response, setResponse] = useState<CommitSearchResponse | null>(null);
	const [error, setError] = useState<string | null>(null);
	const [isLoading, setIsLoading] = useState(false);
	const normalizedQuery = query.trim();
	const normalizedAuthor = filters.author.trim();
	const normalizedScope = filters.scope.trim();
	const boundaries = toSearchBoundaries(filters);
	const canSearch =
		(normalizedQuery.length >= MINIMUM_QUERY_LENGTH ||
			hasCommitSearchFilter(filters)) &&
		(normalizedAuthor.length === 0 ||
			normalizedAuthor.length >= MINIMUM_QUERY_LENGTH) &&
		isCommitSearchDateRangeValid(filters);
	const requestIsValid = canSearch && scopeIsValid;

	useEffect(() => {
		if (!enabled || !repositoryId || !requestIsValid) {
			setResponse(null);
			setError(null);
			setIsLoading(false);
			return;
		}

		let active = true;
		setIsLoading(true);
		setError(null);
		const timer = window.setTimeout(() => {
			void sendRequestWithResponse(
				{
					arguments: {
						afterUnixSeconds: boundaries.afterUnixSeconds,
						author: normalizedAuthor,
						beforeUnixSeconds: boundaries.beforeUnixSeconds,
						deep,
						knownRepositoryId: repositoryId,
						limit: deep ? 100 : 50,
						query: normalizedQuery,
						scope: normalizedScope,
					},
					commandType: "SearchCommits",
				},
				deep ? { timeoutMs: 12_000 } : undefined,
			)
				.then((nextResponse) => {
					if (active) setResponse(nextResponse);
				})
				.catch((reason) => {
					if (active) {
						setResponse(null);
						setError(
							reason instanceof Error
								? reason.message
								: "Commit search failed.",
						);
					}
				})
				.finally(() => {
					if (active) setIsLoading(false);
				});
		}, SEARCH_DEBOUNCE_MS);

		return () => {
			active = false;
			window.clearTimeout(timer);
		};
	}, [
		boundaries.afterUnixSeconds,
		boundaries.beforeUnixSeconds,
		requestIsValid,
		deep,
		enabled,
		normalizedAuthor,
		normalizedQuery,
		normalizedScope,
		repositoryId,
	]);

	return {
		error,
		isLoading,
		minimumQueryLength: MINIMUM_QUERY_LENGTH,
		response,
	};
}
