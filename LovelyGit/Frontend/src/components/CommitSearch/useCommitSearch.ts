import { useEffect, useState } from "react";
import type { CommitSearchResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";

const SEARCH_DEBOUNCE_MS = 140;
const MINIMUM_QUERY_LENGTH = 2;

export function useCommitSearch(
	repositoryId: string | null,
	query: string,
	enabled: boolean,
	deep = false,
) {
	const [response, setResponse] = useState<CommitSearchResponse | null>(null);
	const [error, setError] = useState<string | null>(null);
	const [isLoading, setIsLoading] = useState(false);
	const normalizedQuery = query.trim();

	useEffect(() => {
		if (
			!enabled ||
			!repositoryId ||
			normalizedQuery.length < MINIMUM_QUERY_LENGTH
		) {
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
						deep,
						knownRepositoryId: repositoryId,
						limit: 50,
						query: normalizedQuery,
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
	}, [deep, enabled, normalizedQuery, repositoryId]);

	return {
		error,
		isLoading,
		minimumQueryLength: MINIMUM_QUERY_LENGTH,
		response,
	};
}
