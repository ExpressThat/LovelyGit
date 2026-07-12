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
import {
	cacheCompleteWorkingTreeSummary,
	getCachedWorkingTreeSummary,
	invalidateWorkingTreeSummary,
	setCachedWorkingTreeSummary,
} from "./workingTreeSummaryCache";

const MAX_PRELOADED_CHANGES = 500;
export const BACKGROUND_FULL_PRELOAD_DELAY_MS = 1_500;
export const CACHED_SUMMARY_REFRESH_DELAY_MS = 500;

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
		let fullLoadTimer: number | null = null;
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
				setCachedWorkingTreeSummary(repositoryId, summary);
				setIsDirty(false);
				setHasSummaryLoaded(true);
				if (!summary.isComplete) {
					fullLoadTimer = window.setTimeout(async () => {
						fullLoadTimer = null;
						if (
							previousRepositoryIdRef.current !== repositoryId ||
							loadGeneration !== invalidationGeneration
						) {
							return;
						}
						isLoading = true;
						try {
							const changes = await loadWorkingTreeChanges(repositoryId);
							if (
								previousRepositoryIdRef.current !== repositoryId ||
								loadGeneration !== invalidationGeneration
							) {
								reloadAgain = true;
								return;
							}
							setSummaryCount(changes.totalCount);
							cacheCompleteWorkingTreeSummary(repositoryId, changes.totalCount);
							if (changes.totalCount <= MAX_PRELOADED_CHANGES) {
								setState({ status: "loaded", changes });
								hasFreshChangesRef.current = true;
							}
						} catch {
							if (previousRepositoryIdRef.current === repositoryId) {
								setIsDirty(true);
								setHasSummaryLoaded(false);
							}
						} finally {
							isLoading = false;
							if (
								previousRepositoryIdRef.current === repositoryId &&
								reloadAgain
							) {
								reloadAgain = false;
								scheduleLoad();
							}
						}
					}, BACKGROUND_FULL_PRELOAD_DELAY_MS);
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

		const scheduleLoad = (delay = 150) => {
			if (reloadTimerRef.current != null) clearTimeout(reloadTimerRef.current);
			if (fullLoadTimer != null) {
				clearTimeout(fullLoadTimer);
				fullLoadTimer = null;
			}
			reloadTimerRef.current = window.setTimeout(() => {
				reloadTimerRef.current = null;
				void loadSummary();
			}, delay);
		};

		const cached = getCachedWorkingTreeSummary(repositoryId);
		if (cached) {
			setSummaryCount(cached.totalCount);
			setIsDirty(false);
			setHasSummaryLoaded(true);
			if (!cached.isComplete) {
				scheduleLoad(CACHED_SUMMARY_REFRESH_DELAY_MS);
			}
		} else {
			void loadSummary();
		}
		const unsubscribe = subscribeToServerEvent(
			"WorkingTreeChanged",
			(event) => {
				invalidationGeneration++;
				invalidateWorkingTreeSummary(repositoryId);
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
			if (fullLoadTimer != null) clearTimeout(fullLoadTimer);
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
