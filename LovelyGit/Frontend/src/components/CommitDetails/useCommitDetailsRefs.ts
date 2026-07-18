import { useCallback, useEffect, useMemo, useSyncExternalStore } from "react";
import type { RepositoryRefItem } from "@/generated/types";
import {
	getCachedRepositoryRefs,
	loadRepositoryRefs,
	subscribeRepositoryRefs,
} from "@/lib/repositoryRefsCache";

export function useCommitDetailsRefs(
	repositoryId: string,
	commitHash: string,
): RepositoryRefItem[] {
	const subscribe = useCallback(
		(listener: () => void) => subscribeRepositoryRefs(repositoryId, listener),
		[repositoryId],
	);
	const getSnapshot = useCallback(
		() => getCachedRepositoryRefs(repositoryId),
		[repositoryId],
	);
	const response = useSyncExternalStore(subscribe, getSnapshot, () => null);

	useEffect(() => {
		if (!response) void loadRepositoryRefs(repositoryId).catch(() => undefined);
	}, [repositoryId, response]);

	return useMemo(
		() =>
			(response?.refs ?? []).filter(
				(ref) => ref.commitHash === commitHash && ref.kind !== "Stash",
			),
		[commitHash, response?.refs],
	);
}
