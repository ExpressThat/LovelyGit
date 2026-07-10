import { useEffect, useState } from "react";
import type { RepositoryRefItem } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";

export function useLocalBranches(
	menuOpen: boolean,
	repositoryId: string | null,
) {
	const [branches, setBranches] = useState<RepositoryRefItem[]>([]);
	const [error, setError] = useState<string | null>(null);
	const [isLoading, setIsLoading] = useState(false);

	useEffect(() => {
		if (!menuOpen || !repositoryId) return;

		let isActive = true;
		setIsLoading(true);
		setError(null);
		sendRequestWithResponse({
			arguments: { knownRepositoryId: repositoryId },
			commandType: NativeMessageType.GetRepositoryRefs,
		})
			.then((response) => {
				if (isActive) {
					setBranches(response.refs.filter((ref) => ref.kind === "Local"));
				}
			})
			.catch((loadError) => {
				if (isActive) {
					setError(
						loadError instanceof Error
							? loadError.message
							: "Failed to load branches.",
					);
				}
			})
			.finally(() => {
				if (isActive) setIsLoading(false);
			});

		return () => {
			isActive = false;
		};
	}, [menuOpen, repositoryId]);

	return { branches, error, isLoading };
}
