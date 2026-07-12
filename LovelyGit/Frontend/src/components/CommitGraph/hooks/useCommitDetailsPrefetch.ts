import { useEffect, useRef } from "react";
import { prefetchCommitDetails } from "@/lib/commitDetailsCache";

const HOVER_INTENT_DELAY_MS = 120;

export function useCommitDetailsPrefetch(
	repositoryId: string | null,
	commitHash: string | null,
) {
	const timerRef = useRef<number | null>(null);
	const cancel = () => {
		if (timerRef.current == null) return;
		window.clearTimeout(timerRef.current);
		timerRef.current = null;
	};
	useEffect(
		() => () => {
			if (timerRef.current != null) window.clearTimeout(timerRef.current);
		},
		[],
	);

	return {
		cancel,
		start: () => {
			if (!repositoryId || !commitHash || timerRef.current != null) return;
			timerRef.current = window.setTimeout(() => {
				timerRef.current = null;
				void prefetchCommitDetails(repositoryId, commitHash);
			}, HOVER_INTENT_DELAY_MS);
		},
	};
}
