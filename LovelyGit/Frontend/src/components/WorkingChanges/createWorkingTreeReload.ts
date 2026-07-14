import type { Dispatch, RefObject, SetStateAction } from "react";
import { loadWorkingTreeChanges } from "./WorkingTreeChangesRequests";
import {
	getWorkingTreeLoadErrorMessage,
	type WorkingTreeChangesState,
	type WorkingTreeReloadRequest,
} from "./WorkingTreeChangesState";
import { setCachedWorkingTreeChanges } from "./workingTreeChangesCache";
import { cacheCompleteWorkingTreeSummary } from "./workingTreeSummaryCache";

export function createWorkingTreeReload(options: ReloadOptions) {
	return () => {
		const repositoryId = options.repositoryId;
		if (!repositoryId) return Promise.resolve();
		const existing = options.reloadRequestRef.current;
		if (existing?.repositoryId === repositoryId) return existing.promise;

		options.setIsReloading(true);
		const promise = loadWorkingTreeChanges(repositoryId, (changes) => {
			if (options.previousRepositoryIdRef.current === repositoryId) {
				options.setState({ status: "loaded", changes });
			}
		})
			.then((changes) => {
				if (options.previousRepositoryIdRef.current !== repositoryId) return;
				options.setState({ status: "loaded", changes });
				options.setSummaryCount(changes.totalCount);
				cacheCompleteWorkingTreeSummary(repositoryId, changes.totalCount);
				setCachedWorkingTreeChanges(repositoryId, changes);
				options.setIsDirty(false);
				options.setHasSummaryLoaded(true);
				options.hasFreshChangesRef.current = true;
			})
			.catch((error: unknown) => {
				if (options.previousRepositoryIdRef.current === repositoryId) {
					options.setState((current) => ({
						status: "error",
						changes: current.changes,
						message: getWorkingTreeLoadErrorMessage(error),
					}));
				}
				throw error;
			})
			.finally(() => {
				if (options.reloadRequestRef.current?.promise === promise) {
					options.reloadRequestRef.current = null;
					options.setIsReloading(false);
				}
			});
		options.reloadRequestRef.current = { promise, repositoryId };
		return promise;
	};
}

type Setter<T> = Dispatch<SetStateAction<T>>;
type ReloadOptions = {
	hasFreshChangesRef: RefObject<boolean>;
	previousRepositoryIdRef: RefObject<string | null>;
	reloadRequestRef: RefObject<WorkingTreeReloadRequest | null>;
	repositoryId: string | null;
	setHasSummaryLoaded: Setter<boolean>;
	setIsDirty: Setter<boolean>;
	setIsReloading: Setter<boolean>;
	setState: Setter<WorkingTreeChangesState>;
	setSummaryCount: Setter<number>;
};
