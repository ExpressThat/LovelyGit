import { useEffect, useState } from "react";
import type { FileHistoryResponse } from "@/generated/types";
import {
	sendRequestWithoutResponse,
	sendRequestWithResponse,
} from "@/lib/commands";

export function useFileHistory(
	repositoryId: string | null,
	path: string | null,
	startCommitHash: string | null,
	enabled: boolean,
	deep: boolean,
) {
	const [response, setResponse] = useState<FileHistoryResponse | null>(null);
	const [error, setError] = useState<string | null>(null);
	const [isLoading, setIsLoading] = useState(false);

	useEffect(() => {
		if (!enabled || !repositoryId) return;
		return () => {
			sendRequestWithoutResponse({
				arguments: { knownRepositoryId: repositoryId },
				commandType: "CancelFileHistory",
			});
		};
	}, [enabled, repositoryId]);

	useEffect(() => {
		if (!enabled || !repositoryId || !path) {
			setResponse(null);
			setError(null);
			setIsLoading(false);
			return;
		}

		let active = true;
		setIsLoading(true);
		setError(null);
		void sendRequestWithResponse(
			{
				arguments: {
					deep,
					knownRepositoryId: repositoryId,
					limit: deep ? 250 : 100,
					path,
					startCommitHash,
				},
				commandType: "GetFileHistory",
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
						reason instanceof Error ? reason.message : "File history failed.",
					);
				}
			})
			.finally(() => {
				if (active) setIsLoading(false);
			});

		return () => {
			active = false;
		};
	}, [deep, enabled, path, repositoryId, startCommitHash]);

	return { error, isLoading, response };
}
