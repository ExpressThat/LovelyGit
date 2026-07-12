import {
	type Dispatch,
	type RefObject,
	type SetStateAction,
	useEffect,
	useRef,
} from "react";
import { subscribeToServerEvent } from "@/lib/commands";
import {
	applyObservedWorkingTreeChanges,
	shouldApplyObservedWorkingTreeChanges,
} from "./OptimisticWorkingTreeChanges";
import {
	loadWorkingTreeChangeSummary,
	loadWorkingTreeChanges,
} from "./WorkingTreeChangesRequests";
import type { WorkingTreeChangesState } from "./WorkingTreeChangesState";

const MAX_PRELOADED_CHANGES = 500;

export function useWorkingTreePreload({
	enabled,
	hasFreshChangesRef,
	previousRepositoryIdRef,
	repositoryId,
	setHasSummaryLoaded,
	setIsDirty,
	setState,
	setSummaryCount,
}: PreloadOptions) {
	const reloadTimerRef = useRef<number | null>(null);
	useEffect(() => {
		if (!repositoryId || enabled) return;

		let isLoading = false;
		let reloadAgain = false;
		let invalidationGeneration = 0;
		const loadSummary = async () => {
			if (isLoading) {
				reloadAgain = true;
				return;
			}

			isLoading = true;
			const loadGeneration = invalidationGeneration;
			try {
				const summary = await loadWorkingTreeChangeSummary(repositoryId, true);
				if (
					previousRepositoryIdRef.current !== repositoryId ||
					loadGeneration !== invalidationGeneration
				) {
					reloadAgain = true;
					return;
				}
				setSummaryCount(summary.totalCount);
				setIsDirty(false);
				setHasSummaryLoaded(true);
				if (!summary.isComplete) {
					const changes = await loadWorkingTreeChanges(repositoryId);
					if (
						previousRepositoryIdRef.current !== repositoryId ||
						loadGeneration !== invalidationGeneration
					) {
						reloadAgain = true;
						return;
					}
					setSummaryCount(changes.totalCount);
					if (changes.totalCount <= MAX_PRELOADED_CHANGES) {
						setState({ status: "loaded", changes });
						hasFreshChangesRef.current = true;
					}
				}
			} catch {
				if (previousRepositoryIdRef.current === repositoryId) {
					setIsDirty(true);
					setHasSummaryLoaded(false);
				}
			} finally {
				isLoading = false;
				if (previousRepositoryIdRef.current === repositoryId && reloadAgain) {
					reloadAgain = false;
					scheduleLoad();
				}
			}
		};

		const scheduleLoad = () => {
			if (reloadTimerRef.current != null) clearTimeout(reloadTimerRef.current);
			reloadTimerRef.current = window.setTimeout(() => {
				reloadTimerRef.current = null;
				void loadSummary();
			}, 150);
		};

		void loadSummary();
		const unsubscribe = subscribeToServerEvent(
			"WorkingTreeChanged",
			(event) => {
				invalidationGeneration++;
				hasFreshChangesRef.current = false;
				setIsDirty(true);
				if (shouldApplyObservedWorkingTreeChanges(event.observedChanges)) {
					setState((current) => {
						const changes = applyObservedWorkingTreeChanges(
							current.changes,
							event.observedChanges,
						);
						if (!changes) return current;
						setSummaryCount(changes.totalCount);
						return { status: "loaded", changes };
					});
				}
				scheduleLoad();
			},
		);

		return () => {
			if (reloadTimerRef.current != null) {
				clearTimeout(reloadTimerRef.current);
				reloadTimerRef.current = null;
			}
			unsubscribe();
		};
	}, [
		enabled,
		hasFreshChangesRef,
		previousRepositoryIdRef,
		repositoryId,
		setHasSummaryLoaded,
		setIsDirty,
		setState,
		setSummaryCount,
	]);
}

type StateSetter<T> = Dispatch<SetStateAction<T>>;
type PreloadOptions = {
	enabled: boolean;
	hasFreshChangesRef: RefObject<boolean>;
	previousRepositoryIdRef: RefObject<string | null>;
	repositoryId: string | null;
	setHasSummaryLoaded: StateSetter<boolean>;
	setIsDirty: StateSetter<boolean>;
	setState: StateSetter<WorkingTreeChangesState>;
	setSummaryCount: StateSetter<number>;
};
