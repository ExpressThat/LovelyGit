import { useEffect, useState } from "react";
import type { BranchComparisonResponse } from "@/generated/types";
import { expandBranchComparisonPayload } from "@/lib/branchComparisonPayload";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";

export function useCommitComparison(
	repositoryId: string | null,
	currentCommitHash: string,
	targetCommitHash: string,
) {
	const [comparison, setComparison] = useState<BranchComparisonResponse | null>(
		null,
	);
	const [error, setError] = useState<string | null>(null);
	const [isLoading, setIsLoading] = useState(false);
	const [retryToken, setRetryToken] = useState(0);

	useEffect(() => {
		if (!repositoryId) return;
		void retryToken;
		let active = true;
		setError(null);
		setIsLoading(true);
		void sendRequestWithResponse({
			arguments: {
				currentCommitHash,
				repositoryId,
				targetBranchName: "",
				targetCommitHash,
			},
			commandType: NativeMessageType.GetBranchComparison,
		})
			.then(expandBranchComparisonPayload)
			.then((result) => active && setComparison(result))
			.catch((reason) => {
				if (active) {
					setError(
						reason instanceof Error
							? reason.message
							: "Could not compare these commits.",
					);
				}
			})
			.finally(() => active && setIsLoading(false));
		return () => {
			active = false;
		};
	}, [currentCommitHash, repositoryId, retryToken, targetCommitHash]);

	return {
		comparison,
		error,
		isLoading,
		retry: () => setRetryToken((token) => token + 1),
	};
}
