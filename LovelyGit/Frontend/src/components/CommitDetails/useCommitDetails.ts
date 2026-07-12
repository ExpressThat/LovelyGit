import { useEffect, useState } from "react";
import type { CommitDetailsResponse } from "@/generated/types";
import {
	getCachedCommitDetails,
	loadCommitDetails,
} from "@/lib/commitDetailsCache";

export type CommitDetailsState =
	| { status: "loading" }
	| { status: "error"; message: string }
	| {
			status: "loaded";
			details: CommitDetailsResponse;
			isRefreshing: boolean;
			refreshError: string | null;
	  };

export function useCommitDetails(
	repositoryId: string,
	commitHash: string,
	parentIndex: number,
	refreshToken: number,
) {
	const [retryToken, setRetryToken] = useState(0);
	const [state, setState] = useState<CommitDetailsState>(() =>
		cachedState(repositoryId, commitHash, parentIndex),
	);

	useEffect(() => {
		void refreshToken;
		void retryToken;
		let isActive = true;
		const cached = getCachedCommitDetails(
			repositoryId,
			commitHash,
			parentIndex,
		);
		if (cached) {
			setState({
				status: "loaded",
				details: cached,
				isRefreshing: false,
				refreshError: null,
			});
			return;
		}
		setState((current) =>
			current.status === "loaded"
				? { ...current, isRefreshing: true, refreshError: null }
				: { status: "loading" },
		);
		loadCommitDetails(repositoryId, commitHash, parentIndex)
			.then((details) => {
				if (!isActive) return;
				if (!details) {
					setFailure("Commit details were empty.");
					return;
				}
				setState({
					status: "loaded",
					details,
					isRefreshing: false,
					refreshError: null,
				});
			})
			.catch((error: unknown) => {
				if (!isActive) return;
				setFailure(message(error));
			});

		function setFailure(failure: string) {
			setState((current) =>
				current.status === "loaded"
					? { ...current, isRefreshing: false, refreshError: failure }
					: { status: "error", message: failure },
			);
		}

		return () => {
			isActive = false;
		};
	}, [commitHash, parentIndex, refreshToken, repositoryId, retryToken]);

	return { retry: () => setRetryToken((token) => token + 1), state };
}

function cachedState(
	repositoryId: string,
	commitHash: string,
	parentIndex: number,
): CommitDetailsState {
	const details = getCachedCommitDetails(repositoryId, commitHash, parentIndex);
	return details
		? { status: "loaded", details, isRefreshing: false, refreshError: null }
		: { status: "loading" };
}

function message(error: unknown) {
	return error instanceof Error
		? error.message
		: "Failed to load commit details.";
}
