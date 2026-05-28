import { startTransition, useEffect, useEffectEvent, useState } from "react";
import {
	CommsHubCommandType,
	sendRequestWithResponse,
} from "@/lib/registerSignalR";
import type { CommitGraphResponse, CommitGraphRow } from "../types/graph";

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
	hasMore: boolean;
	laneCount: number;
	loading: boolean;
	nextCursor: string | null;
	requestedEnd: number;
	rows: Array<CommitGraphRow | null>;
	totalRows: number;
};

const session: GraphSession = {
	hasMore: true,
	laneCount: 0,
	loading: false,
	nextCursor: null,
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

	const runLoader = useEffectEvent(async () => {
		if (session.loading) {
			return;
		}

		const requiredLength = session.requestedEnd + PAGE_SIZE * PREFETCH_PAGES;
		if (!session.hasMore || session.rows.length >= requiredLength) {
			return;
		}

		session.loading = true;
		setState((current) => ({ ...current, error: null }));

		try {
			let loadedLength = session.rows.length;
			while (session.hasMore && loadedLength < requiredLength) {
				const response = await sendRequestWithResponse<CommitGraphResponse>({
					commandType: CommsHubCommandType.CommitGraph,
					Arguments: {
						cursor: session.nextCursor,
						limit: PAGE_SIZE.toString(),
					},
				});

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
			session.loading = false;
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

	// biome-ignore lint/correctness/useExhaustiveDependencies: this is intended
	useEffect(() => {
		if (session.rows.length > 0) {
			return;
		}
		session.requestedEnd = PAGE_SIZE;
		void runLoader();
	}, [runLoader]);

	return {
		...state,
		ensureRangeLoaded,
	};
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
