import { useEffect, useState } from "react";
import type { RepositoryRefItem } from "@/generated/types";
import {
	getCachedRepositoryRefs,
	loadRepositoryRefs,
} from "@/lib/repositoryRefsCache";

export function useCommitSearchRefs(
	repositoryId: string | null,
	enabled: boolean,
) {
	const [refs, setRefs] = useState<RepositoryRefItem[]>([]);
	const [isLoading, setIsLoading] = useState(false);
	const [loadFailed, setLoadFailed] = useState(false);

	useEffect(() => {
		if (!enabled || !repositoryId) return;
		let active = true;
		const cached = getCachedRepositoryRefs(repositoryId);
		if (cached) {
			setRefs(cached.refs);
			setLoadFailed(false);
			return;
		}

		setIsLoading(true);
		setLoadFailed(false);
		void loadRepositoryRefs(repositoryId)
			.then((response) => {
				if (active) setRefs(response.refs);
			})
			.catch(() => {
				if (active) {
					setRefs([]);
					setLoadFailed(true);
				}
			})
			.finally(() => {
				if (active) setIsLoading(false);
			});
		return () => {
			active = false;
		};
	}, [enabled, repositoryId]);

	return { isLoading, loadFailed, refs };
}
