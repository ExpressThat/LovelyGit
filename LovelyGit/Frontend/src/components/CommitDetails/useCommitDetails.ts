import { useEffect, useState } from "react";
import type { CommitDetailsResponse } from "@/generated/types";
import { loadCommitDetails } from "@/lib/commitDetailsCache";

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
	const [state, setState] = useState<CommitDetailsState>({ status: "loading" });

	useEffect(() => {
		void refreshToken;
		void retryToken;
		let isActive = true;
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

function message(error: unknown) {
	return error instanceof Error
		? error.message
		: "Failed to load commit details.";
}
