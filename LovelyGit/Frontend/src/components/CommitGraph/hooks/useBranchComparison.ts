import { useEffect, useState } from "react";
import type { BranchComparisonResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";

export function useBranchComparison(
	repositoryId: string | null,
	targetBranchName: string | null,
) {
	const [comparison, setComparison] = useState<BranchComparisonResponse | null>(
		null,
	);
	const [error, setError] = useState<string | null>(null);
	const [isLoading, setIsLoading] = useState(false);

	useEffect(() => {
		if (!repositoryId || !targetBranchName) return;
		let active = true;
		setComparison(null);
		setError(null);
		setIsLoading(true);
		void sendRequestWithResponse({
			arguments: { repositoryId, targetBranchName },
			commandType: NativeMessageType.GetBranchComparison,
		})
			.then((result) => {
				if (active) setComparison(result);
			})
			.catch((reason) => {
				if (active) {
					setError(
						reason instanceof Error
							? reason.message
							: "Could not compare these branches.",
					);
				}
			})
			.finally(() => {
				if (active) setIsLoading(false);
			});
		return () => {
			active = false;
		};
	}, [repositoryId, targetBranchName]);

	return { comparison, error, isLoading };
}
