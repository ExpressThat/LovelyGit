import { startTransition, useEffect, useEffectEvent, useRef, useState } from "react";
import type { CommitGraphResponse, CommitGraphRow } from "../types/graph";
import { CommsHubCommandType, sendRequestWithResponse } from "@/lib/registerSignalR";

const PAGE_SIZE = 400;
const PREFETCH_PAGES = 1;

type CommitGraphState = {
	error: string | null;
	isInitialLoading: boolean;
	isPageLoading: boolean;
	laneCount: number;
	rows: Array<CommitGraphRow | null>;
	totalRows: number;
};

type GraphSessionCache = {
	hasMore: boolean;
	laneCount: number;
	nextCursor: string | null;
	rows: Array<CommitGraphRow | null>;
	totalRows: number;
};

const sessionCache: GraphSessionCache = {
	hasMore: true,
	laneCount: 0,
	nextCursor: null,
	rows: [],
	totalRows: 0,
};

export function useCommitGraphData() {
	const [state, setState] = useState<CommitGraphState>({
		error: null,
		isInitialLoading: sessionCache.rows.length === 0,
		isPageLoading: false,
		laneCount: sessionCache.laneCount,
		rows: sessionCache.rows,
		totalRows: sessionCache.hasMore
			? Math.min(sessionCache.totalRows, sessionCache.rows.length + PAGE_SIZE)
			: sessionCache.totalRows,
	});

	const hasMoreRef = useRef(sessionCache.hasMore);
	const loadingRef = useRef(false);
	const nextCursorRef = useRef<string | null>(sessionCache.nextCursor);
	const requestedEndRef = useRef(0);
	const stateRef = useRef(state);
	stateRef.current = state;

	const runLoader = useEffectEvent(async () => {
		if (loadingRef.current) {
			return;
		}

		const requiredLength = requestedEndRef.current + PAGE_SIZE * PREFETCH_PAGES;
		if (!hasMoreRef.current || stateRef.current.rows.length >= requiredLength) {
			return;
		}

		loadingRef.current = true;
		setState((current) => ({ ...current, isPageLoading: true, error: null }));

		try {
			let loadedLength = stateRef.current.rows.length;
			while (hasMoreRef.current && loadedLength < requiredLength) {
				const response = await sendRequestWithResponse<CommitGraphResponse>({
					commandType: CommsHubCommandType.CommitGraph,
					Arguments: {
						cursor: nextCursorRef.current,
						limit: PAGE_SIZE.toString()
					}
				});

				if (!response) {
					continue;
				}

				nextCursorRef.current = response.nextCursor;
				hasMoreRef.current = response.hasMore;
				const responseEnd = response.rows.reduce(
					(max, row) => Math.max(max, row.rowIndex + 1),
					loadedLength,
				);
				loadedLength = Math.max(loadedLength, responseEnd);
				const visibleTotal = response.hasMore
					? Math.max(loadedLength + PAGE_SIZE, requiredLength)
					: Math.max(response.totalRows, loadedLength);

				startTransition(() => {
					setState((current) => {
						const nextRows = current.rows.slice();
						for (const row of response.rows) {
							nextRows[row.rowIndex] = row;
						}

						return {
							...current,
							error: null,
							isInitialLoading: false,
							isPageLoading: true,
							laneCount: Math.max(current.laneCount, response.laneCount),
							rows: nextRows,
							totalRows: response.hasMore
								? Math.max(current.totalRows, visibleTotal)
								: visibleTotal,
						};
					});
				});

				if (response.rows.length === 0) {
					hasMoreRef.current = false;
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
			loadingRef.current = false;
			setState((current) =>
				current.isPageLoading
					? {
						...current,
						isPageLoading: false,
					}
					: current,
			);
		}
	});

	const ensureRangeLoaded = useEffectEvent((startIndex: number, endIndex: number) => {
		if (endIndex < startIndex) {
			return;
		}

		requestedEndRef.current = Math.max(requestedEndRef.current, endIndex);
		void runLoader();
	});

	useEffect(() => {
		sessionCache.hasMore = hasMoreRef.current;
		sessionCache.laneCount = state.laneCount;
		sessionCache.nextCursor = nextCursorRef.current;
		sessionCache.rows = state.rows;
		sessionCache.totalRows = state.totalRows;
	}, [state.laneCount, state.rows, state.totalRows]);

	const reloadGraph = useEffectEvent(() => {
		hasMoreRef.current = true;
		loadingRef.current = false;
		nextCursorRef.current = null;
		requestedEndRef.current = 0;

		setState({
			error: null,
			isInitialLoading: true,
			isPageLoading: false,
			laneCount: 0,
			rows: [],
			totalRows: 0,
		});

		void runLoader();
	});

	// biome-ignore lint/correctness/useExhaustiveDependencies: this is intended
	useEffect(() => {
		if (sessionCache.rows.length > 0) {
			return;
		}
		requestedEndRef.current = PAGE_SIZE;
		void runLoader();
	}, [runLoader]);

	return {
		...state,
		ensureRangeLoaded,
		reloadGraph,
	};
}
