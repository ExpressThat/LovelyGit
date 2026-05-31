import { startTransition, useEffect, useEffectEvent, useState } from "react";
import type {
	CommitGraphResponse,
	CommitGraphRow,
} from "@/generated/ExpressThat.LovelyGit.Services.Git.CommitGraph.Models";
import { sendRequestWithResponse } from "@/lib/registerSignalR";
import { useSetting } from "@/lib/settings/settingsStore";

const PAGE_SIZE = 400;
const PREFETCH_PAGES = 1;

type CommitGraphState = {
	error: string | null;
	isInitialLoading: boolean;
	laneCount: number;
	rows: Array<CommitGraphRow | null>;
	totalRows: number;
};

type GraphSession = {
	generation: number;
	hasMore: boolean;
	laneCount: number;
	loading: boolean;
	nextCursor?: string;
	repositoryId: string | null;
	requestedEnd: number;
	rows: Array<CommitGraphRow | null>;
	totalRows: number;
};

const session: GraphSession = {
	generation: 0,
	hasMore: true,
	laneCount: 0,
	loading: false,
	nextCursor: undefined,
	repositoryId: null,
	requestedEnd: 0,
	rows: [],
	totalRows: 0,
};

export function useCommitGraphData() {
	const [state, setState] = useState<CommitGraphState>(() => ({
		error: null,
		isInitialLoading: session.rows.length === 0,
		laneCount: session.laneCount,
		rows: session.rows,
		totalRows: visibleTotal(),
	}));

	const currentGitRepositoryId = useSetting("CurrentGitRepositoryId");

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
		if (!session.hasMore || session.rows.length >= requiredLength) {
			return;
		}

		session.loading = true;
		setState((current) => ({ ...current, error: null }));

		try {
			let loadedLength = session.rows.length;
			while (session.hasMore && loadedLength < requiredLength) {
				const response = await sendRequestWithResponse({
					commandType: "CommitGraph",
					arguments: {
						knownRepositoryId: loadingRepositoryId,
						cursor: session.nextCursor || undefined,
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

	// biome-ignore lint/correctness/useExhaustiveDependencies: runLoader is an effect event; reset only when the repo changes.
	useEffect(() => {
		resetSession(currentGitRepositoryId);
		setState({
			error: null,
			isInitialLoading: Boolean(currentGitRepositoryId),
			laneCount: 0,
			rows: [],
			totalRows: currentGitRepositoryId ? PAGE_SIZE : 0,
		});

		if (!currentGitRepositoryId) {
			return;
		}

		session.requestedEnd = PAGE_SIZE;
		void runLoader();
	}, [currentGitRepositoryId]);

	return {
		...state,
		ensureRangeLoaded,
	};
}

function resetSession(repositoryId: string | null) {
	session.generation++;
	session.hasMore = true;
	session.laneCount = 0;
	session.loading = false;
	session.nextCursor = undefined;
	session.repositoryId = repositoryId;
	session.requestedEnd = 0;
	session.rows = [];
	session.totalRows = 0;
}

function applyResponse(response: CommitGraphResponse, requiredLength: number) {
	const nextRows = session.rows.slice();
	for (const row of response.rows) {
		nextRows[row.rowIndex] = row;
	}

	session.nextCursor = response.nextCursor;
	session.hasMore = response.hasMore;
	session.rows = nextRows;
	session.laneCount = Math.max(session.laneCount, response.laneCount);
	session.totalRows = response.hasMore
		? Math.max(session.totalRows, nextRows.length + PAGE_SIZE, requiredLength)
		: Math.max(response.totalRows, nextRows.length);
}

function readSessionState(): CommitGraphState {
	return {
		error: null,
		isInitialLoading: false,
		laneCount: session.laneCount,
		rows: session.rows,
		totalRows: visibleTotal(),
	};
}

function visibleTotal() {
	return session.hasMore
		? Math.min(session.totalRows, session.rows.length + PAGE_SIZE)
		: session.totalRows;
}
