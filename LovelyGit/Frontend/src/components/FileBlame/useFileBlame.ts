import { useEffect, useState } from "react";
import type { FileBlameResponse } from "@/generated/types";
import {
	sendRequestWithoutResponse,
	sendRequestWithResponse,
} from "@/lib/commands";
import { expandFileBlamePayload } from "./fileBlamePayload";

export function useFileBlame(
	repositoryId: string | null,
	path: string | null,
	startCommitHash: string | null,
	enabled: boolean,
	deep: boolean,
) {
	const [response, setResponse] = useState<FileBlameResponse | null>(null);
	const [error, setError] = useState<string | null>(null);
	const [isLoading, setIsLoading] = useState(false);

	useEffect(() => {
		if (!enabled || !repositoryId) return;
		return () => {
			sendRequestWithoutResponse({
				arguments: { knownRepositoryId: repositoryId },
				commandType: "CancelFileBlame",
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
					path,
					startCommitHash,
				},
				commandType: "GetFileBlame",
			},
			deep ? { timeoutMs: 12_000 } : undefined,
		)
			.then(expandFileBlamePayload)
			.then((nextResponse) => {
				if (active) setResponse(nextResponse);
			})
			.catch((reason) => {
				if (active) {
					setResponse(null);
					setError(
						reason instanceof Error ? reason.message : "File blame failed.",
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
