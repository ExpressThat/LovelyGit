import { startTransition, useEffect, useEffectEvent, useRef, useState } from "react";
import type { CommitGraphResponse, CommitGraphRow } from "../types/graph";

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

	const normalizeStringArray = (value: unknown): string[] =>
		Array.isArray(value) ? value.filter((item) => typeof item === "string") : [];

	const normalizeCommit = (rawCommit: any) => ({
		hash: rawCommit?.hash ?? rawCommit?.Hash ?? "",
		parents: normalizeStringArray(rawCommit?.parents ?? rawCommit?.Parents),
		author: rawCommit?.author ?? rawCommit?.Author ?? "",
		email: rawCommit?.email ?? rawCommit?.Email ?? "",
		date: rawCommit?.date ?? rawCommit?.Date ?? 0,
		message: rawCommit?.message ?? rawCommit?.Message ?? "",
		branches: normalizeStringArray(rawCommit?.branches ?? rawCommit?.Branches),
		tags: normalizeStringArray(rawCommit?.tags ?? rawCommit?.Tags),
		stats: rawCommit?.stats ?? rawCommit?.Stats ?? null,
	});

	const normalizeResponse = (raw: any): CommitGraphResponse => {
		const rawRows = Array.isArray(raw?.rows)
			? raw.rows
			: Array.isArray(raw?.Rows)
				? raw.Rows
				: [];
		const rows: CommitGraphRow[] = rawRows.map((row: any) => ({
			commit: normalizeCommit(row?.commit ?? row?.Commit),
			row_index: row?.row_index ?? row?.rowIndex ?? row?.RowIndex ?? 0,
			lane: row?.lane ?? row?.Lane ?? 0,
			active_lanes: row?.active_lanes ?? row?.activeLanes ?? row?.ActiveLanes ?? [],
			active_lanes_above:
				row?.active_lanes_above ?? row?.activeLanesAbove ?? row?.ActiveLanesAbove ?? [],
			active_lanes_below:
				row?.active_lanes_below ?? row?.activeLanesBelow ?? row?.ActiveLanesBelow ?? [],
			edges_above: (row?.edges_above ?? row?.edgesAbove ?? row?.EdgesAbove ?? []).map(
				(edge: any) => ({
					from_lane: edge?.from_lane ?? edge?.fromLane ?? edge?.FromLane ?? 0,
					to_lane: edge?.to_lane ?? edge?.toLane ?? edge?.ToLane ?? 0,
					kind: edge?.kind ?? "straight",
				}),
			),
			edges_below: (row?.edges_below ?? row?.edgesBelow ?? row?.EdgesBelow ?? []).map(
				(edge: any) => ({
					from_lane: edge?.from_lane ?? edge?.fromLane ?? edge?.FromLane ?? 0,
					to_lane: edge?.to_lane ?? edge?.toLane ?? edge?.ToLane ?? 0,
					kind: edge?.kind ?? edge?.Kind ?? "straight",
				}),
			),
			is_merge_commit:
				row?.is_merge_commit ?? row?.isMergeCommit ?? row?.IsMergeCommit ?? false,
			is_branch_tip: row?.is_branch_tip ?? row?.isBranchTip ?? row?.IsBranchTip ?? false,
		}));

		return {
			total_rows: raw?.total_rows ?? raw?.totalRows ?? raw?.TotalRows ?? 0,
			lane_count: raw?.lane_count ?? raw?.laneCount ?? raw?.LaneCount ?? 0,
			rows,
			next_cursor: raw?.next_cursor ?? raw?.nextCursor ?? raw?.NextCursor ?? null,
			has_more: raw?.has_more ?? raw?.hasMore ?? raw?.HasMore ?? false,
		};
	};

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
				const fetchResponse = await fetch("/commitGraph", {
					body: JSON.stringify({
						limit: PAGE_SIZE,
						cursor: nextCursorRef.current,
					}),
					headers: {
						"content-type": "application/json",
					},
					method: "POST",
				});
				if (!fetchResponse.ok) {
					throw new Error(`Failed to load commit graph (${fetchResponse.status})`);
				}

				const raw = await fetchResponse.json();
				const response = normalizeResponse(raw);

				nextCursorRef.current = response.next_cursor;
				hasMoreRef.current = response.has_more;
				const responseEnd = response.rows.reduce(
					(max, row) => Math.max(max, row.row_index + 1),
					loadedLength,
				);
				loadedLength = Math.max(loadedLength, responseEnd);
				const visibleTotal = response.has_more
					? Math.max(loadedLength + PAGE_SIZE, requiredLength)
					: Math.max(response.total_rows, loadedLength);

				startTransition(() => {
					setState((current) => {
						const nextRows = current.rows.slice();
						for (const row of response.rows) {
							nextRows[row.row_index] = row;
						}

						return {
							...current,
							error: null,
							isInitialLoading: false,
							isPageLoading: true,
							laneCount: Math.max(current.laneCount, response.lane_count),
							rows: nextRows,
							totalRows: response.has_more
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
