import {
	startTransition,
	useEffect,
	useEffectEvent,
	useRef,
	useState,
} from "react";
import type { CommitGraphRow } from "@/generated/types";
import {
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "@/lib/commands";
import { useSetting } from "@/lib/settings/settingsStore";
import {
	activateCommitGraphSession,
	type CommitGraphState,
	currentSessionRepositoryId,
	deferCachedCommitGraphRefresh,
	session,
} from "./commitGraphSession";
import { applyCommitGraphResponse } from "./commitGraphSessionUpdates";

const PAGE_SIZE = 128;
const PREFETCH_ROWS = 32;
export function useCommitGraphData(externalRefreshToken = 0) {
	const [graphInvalidation, setGraphInvalidation] = useState(0);
	const lifecycleRef = useRef({
		externalRefreshToken,
		graphInvalidation,
		repositoryId: currentSessionRepositoryId(),
	});
	const [state, setState] = useState<CommitGraphState>(() => ({
		currentBranchName: session.currentBranchName,
		error: null,
		isInitialLoading: session.rows.length === 0,
		laneCount: session.laneCount,
		remotePrefixes: session.remotePrefixes,
		remoteRepositoryUrl: session.remoteRepositoryUrl,
		refRowsByHash: session.refRowsByHash,
		rows: session.rows,
		totalRows: visibleTotal(),
	}));
	const currentGitRepositoryId = useSetting("CurrentGitRepositoryId");
	useEffect(() => {
		return subscribeToServerEvent("CommitGraphChanged", () => {
			setGraphInvalidation((generation) => generation + 1);
		});
	}, []);
	const runLoader = useEffectEvent(async () => {
		if (session.loading) {
			return;
		}
		if (!currentGitRepositoryId) {
			return;
		}
		const loadingRepositoryId = currentGitRepositoryId;
		const loadingGeneration = session.generation;
		const requestedEnd = session.requestedEnd;
		const requiredLength = requestedEnd + PREFETCH_ROWS;
		if (!session.hasMore || session.loadedRowCount >= requiredLength) {
			return;
		}
		session.loading = true;
		setState((current) => ({ ...current, error: null }));
		try {
			let loadedLength = session.loadedRowCount;
			while (session.hasMore && loadedLength < requiredLength) {
				const response = await sendRequestWithResponse({
					commandType: "CommitGraph",
					arguments: {
						knownRepositoryId: loadingRepositoryId,
						cursor: session.nextCursor,
						limit: PAGE_SIZE,
					},
				});
				if (
					session.repositoryId !== loadingRepositoryId ||
					session.generation !== loadingGeneration
				) {
					return;
				}
				if (!response) {
					continue;
				}
				applyCommitGraphResponse(response, requiredLength, PAGE_SIZE);
				loadedLength = session.rows.length;
				startTransition(() => {
					setState(readSessionState);
				});
				if (response.rows.length === 0) {
					session.hasMore = false;
					break;
				}
			}
		} catch (error) {
			const message =
				error instanceof Error ? error.message : "Failed to load commit graph";
			setState((current) => ({
				...current,
				error: message,
				isInitialLoading: false,
			}));
		} finally {
			if (
				session.repositoryId === loadingRepositoryId &&
				session.generation === loadingGeneration
			) {
				session.loading = false;
				if (
					session.hasMore &&
					session.requestedEnd > requestedEnd &&
					session.loadedRowCount < session.requestedEnd + PREFETCH_ROWS
				) {
					void runLoader();
				}
			}
		}
	});
	const ensureRangeLoaded = useEffectEvent(
		(startIndex: number, endIndex: number) => {
			if (endIndex < startIndex) {
				return;
			}
			session.requestedEnd = Math.max(session.requestedEnd, endIndex);
			void runLoader();
		},
	);
	useEffect(() => {
		const previous = lifecycleRef.current;
		const repositoryChanged = previous.repositoryId !== currentGitRepositoryId;
		lifecycleRef.current = {
			externalRefreshToken,
			graphInvalidation,
			repositoryId: currentGitRepositoryId,
		};
		if (repositoryChanged) {
			activateCommitGraphSession(currentGitRepositoryId);
			setState({
				error: null,
				currentBranchName: session.currentBranchName,
				isInitialLoading:
					Boolean(currentGitRepositoryId) && session.rows.length === 0,
				laneCount: session.laneCount,
				remotePrefixes: session.remotePrefixes,
				remoteRepositoryUrl: session.remoteRepositoryUrl,
				refRowsByHash: session.refRowsByHash,
				rows: session.rows,
				totalRows:
					session.totalRows || (currentGitRepositoryId ? PAGE_SIZE : 0),
			});
		} else if (
			previous.externalRefreshToken !== externalRefreshToken ||
			previous.graphInvalidation !== graphInvalidation
		) {
			resetSession(currentGitRepositoryId, { keepRows: true });
			setState((current) => ({
				...current,
				error: null,
				isInitialLoading: false,
			}));
		}
		if (!currentGitRepositoryId) {
			return;
		}
		session.requestedEnd = 0;
		if (repositoryChanged && session.rows.length > 0) {
			return deferCachedCommitGraphRefresh(() => void runLoader());
		}
		void runLoader();
	}, [currentGitRepositoryId, externalRefreshToken, graphInvalidation]);
	return {
		...state,
		ensureRangeLoaded,
	};
}
function resetSession(
	repositoryId: string | null,
	{ keepRows }: { keepRows: boolean },
) {
	const previousRows = keepRows ? session.rows : [];
	const previousTotalRows = keepRows ? session.totalRows : 0;
	const previousLaneCount = keepRows ? session.laneCount : 0;
	const previousRemotePrefixes = keepRows ? session.remotePrefixes : [];
	const previousRemoteRepositoryUrl = keepRows
		? session.remoteRepositoryUrl
		: null;
	const previousRefRows = keepRows
		? session.refRowsByHash
		: new Map<string, CommitGraphRow>();
	const previousCurrentBranchName = keepRows ? session.currentBranchName : null;
	session.generation++;
	session.currentBranchName = previousCurrentBranchName;
	session.hasMore = true;
	session.laneCount = previousLaneCount;
	session.loadedRowCount = 0;
	session.loading = false;
	session.nextCursor = null;
	session.remotePrefixes = previousRemotePrefixes;
	session.remoteRepositoryUrl = previousRemoteRepositoryUrl;
	session.refRowsByHash = previousRefRows;
	session.repositoryId = repositoryId;
	session.requestedEnd = 0;
	session.rows = previousRows;
	session.totalRows = previousTotalRows;
}
function readSessionState(): CommitGraphState {
	return {
		currentBranchName: session.currentBranchName,
		error: null,
		isInitialLoading: false,
		laneCount: session.laneCount,
		remotePrefixes: session.remotePrefixes,
		remoteRepositoryUrl: session.remoteRepositoryUrl,
		refRowsByHash: session.refRowsByHash,
		rows: session.rows,
		totalRows: visibleTotal(),
	};
}
function visibleTotal() {
	return session.hasMore
		? Math.min(session.totalRows, session.rows.length + PAGE_SIZE)
		: session.totalRows;
}
