import {
	startTransition,
	useEffect,
	useEffectEvent,
	useRef,
	useState,
} from "react";
import type { CommitGraphResponse } from "@/generated/types";
import {
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "@/lib/commands";
import { useSetting } from "@/lib/settings/settingsStore";
import {
	type CommitGraphState,
	currentSessionRepositoryId,
	session,
} from "./commitGraphSession";
import { compactCommitGraphRow } from "./compactCommitGraphRow";

const PAGE_SIZE = 128;
const PREFETCH_PAGES = 1;
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
		const requiredLength = session.requestedEnd + PAGE_SIZE * PREFETCH_PAGES;
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
				applyResponse(response, requiredLength);
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
			resetSession(currentGitRepositoryId, { keepRows: false });
			setState({
				error: null,
				currentBranchName: null,
				isInitialLoading: Boolean(currentGitRepositoryId),
				laneCount: 0,
				remotePrefixes: [],
				remoteRepositoryUrl: null,
				rows: [],
				totalRows: currentGitRepositoryId ? PAGE_SIZE : 0,
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
		session.requestedEnd = PAGE_SIZE;
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
	session.repositoryId = repositoryId;
	session.requestedEnd = 0;
	session.rows = previousRows;
	session.totalRows = previousTotalRows;
}
function applyResponse(response: CommitGraphResponse, requiredLength: number) {
	const nextRows = session.rows;
	let loadedRowCount = session.loadedRowCount;
	for (const row of response.rows) {
		nextRows[row.rowIndex] = compactCommitGraphRow(row);
		loadedRowCount = Math.max(loadedRowCount, row.rowIndex + 1);
	}
	if (!response.hasMore) {
		nextRows.length = response.totalRows;
	}
	session.nextCursor = response.nextCursor;
	session.hasMore = response.hasMore;
	session.currentBranchName = response.currentBranchName ?? null;
	session.remotePrefixes = response.remotePrefixes;
	session.remoteRepositoryUrl = response.remoteRepositoryUrl;
	session.rows = nextRows;
	session.loadedRowCount = loadedRowCount;
	session.laneCount = Math.max(session.laneCount, response.laneCount);
	session.totalRows = response.hasMore
		? Math.max(session.totalRows, nextRows.length + PAGE_SIZE, requiredLength)
		: response.totalRows;
}
function readSessionState(): CommitGraphState {
	return {
		currentBranchName: session.currentBranchName,
		error: null,
		isInitialLoading: false,
		laneCount: session.laneCount,
		remotePrefixes: session.remotePrefixes,
		remoteRepositoryUrl: session.remoteRepositoryUrl,
		rows: session.rows,
		totalRows: visibleTotal(),
	};
}
function visibleTotal() {
	return session.hasMore
		? Math.min(session.totalRows, session.rows.length + PAGE_SIZE)
		: session.totalRows;
}
