import {
	startTransition,
	useEffect,
	useEffectEvent,
	useRef,
	useState,
} from "react";
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
	laneCount: number;
	loadedPages: Set<number>;
	pageCache: Map<number, CommitGraphResponse>;
	rows: Array<CommitGraphRow | null>;
	totalRows: number;
};

const sessionCache: GraphSessionCache = {
	laneCount: 0,
	loadedPages: new Set<number>(),
	pageCache: new Map<number, CommitGraphResponse>(),
	rows: [],
	totalRows: 0,
};

export function useCommitGraphData() {
	const [state, setState] = useState<CommitGraphState>({
		error: null,
		isInitialLoading: sessionCache.totalRows === 0,
		isPageLoading: false,
		laneCount: sessionCache.laneCount,
		rows: sessionCache.rows,
		totalRows: sessionCache.totalRows,
	});

	const loadedPagesRef = useRef(new Set<number>(sessionCache.loadedPages));
	const loadingPagesRef = useRef(new Set<number>());
	const pendingPagesRef = useRef(new Set<number>());
	const runningRef = useRef(false);
	const centerPageRef = useRef(0);
	const stateRef = useRef(state);
	stateRef.current = state;

	const mergeRows = (
		currentRows: Array<CommitGraphRow | null>,
		response: CommitGraphResponse,
	) => {
		const nextRows =
			currentRows.length === response.total_rows
				? currentRows.slice()
				: Array.from(
						{ length: response.total_rows },
						(_, index) => currentRows[index] ?? null,
					);

		for (const row of response.rows) {
			nextRows[row.row_index] = row;
		}

		return nextRows;
	};

	const normalizeResponse = (raw: any): CommitGraphResponse => {
		const rawRows = Array.isArray(raw?.rows) ? raw.rows : [];
		const rows: CommitGraphRow[] = rawRows.map((row: any) => ({
			commit: row?.commit,
			row_index: row?.row_index ?? row?.rowIndex ?? 0,
			lane: row?.lane ?? 0,
			active_lanes: row?.active_lanes ?? row?.activeLanes ?? [],
			active_lanes_above:
				row?.active_lanes_above ?? row?.activeLanesAbove ?? [],
			active_lanes_below:
				row?.active_lanes_below ?? row?.activeLanesBelow ?? [],
			edges_above: (row?.edges_above ?? row?.edgesAbove ?? []).map(
				(edge: any) => ({
					from_lane: edge?.from_lane ?? edge?.fromLane ?? 0,
					to_lane: edge?.to_lane ?? edge?.toLane ?? 0,
					kind: edge?.kind ?? "straight",
				}),
			),
			edges_below: (row?.edges_below ?? row?.edgesBelow ?? []).map(
				(edge: any) => ({
					from_lane: edge?.from_lane ?? edge?.fromLane ?? 0,
					to_lane: edge?.to_lane ?? edge?.toLane ?? 0,
					kind: edge?.kind ?? "straight",
				}),
			),
			is_merge_commit: row?.is_merge_commit ?? row?.isMergeCommit ?? false,
			is_branch_tip: row?.is_branch_tip ?? row?.isBranchTip ?? false,
		}));

		return {
			total_rows: raw?.total_rows ?? raw?.totalRows ?? 0,
			lane_count: raw?.lane_count ?? raw?.laneCount ?? 0,
			rows,
		};
	};

	const pickNextPage = () => {
		const candidates = Array.from(pendingPagesRef.current).filter(
			(page) =>
				!loadedPagesRef.current.has(page) && !loadingPagesRef.current.has(page),
		);

		if (candidates.length === 0) {
			return null;
		}

		candidates.sort(
			(left, right) =>
				Math.abs(left - centerPageRef.current) -
				Math.abs(right - centerPageRef.current),
		);
		return candidates[0];
	};

	const runQueue = useEffectEvent(async () => {
		if (runningRef.current) {
			return;
		}

		const initialPage = pickNextPage();
		if (initialPage === null) {
			return;
		}

		runningRef.current = true;
		setState((current) => ({
			...current,
			isPageLoading: true,
			error: null,
		}));

		try {
			let nextPage: number | null = initialPage;
			while (true) {
				if (nextPage === null) {
					break;
				}
				const pageIndex = nextPage;

				loadingPagesRef.current.add(pageIndex);
				pendingPagesRef.current.delete(pageIndex);

				try {
					const raw = await fetch(
						`/commitGraph?offset=${pageIndex * PAGE_SIZE}&limit=${PAGE_SIZE}`,
					).then((x) => x.json());
					const response = normalizeResponse(raw);

					sessionCache.pageCache.set(pageIndex, response);
					sessionCache.loadedPages.add(pageIndex);

					loadedPagesRef.current.add(pageIndex);
					startTransition(() => {
						setState((current) => ({
							...current,
							error: null,
							isInitialLoading: false,
							isPageLoading: true,
							laneCount: Math.max(current.laneCount, response.lane_count),
							rows: mergeRows(current.rows, response),
							totalRows: response.total_rows,
						}));
					});
				} catch (error) {
					const message =
						error instanceof Error
							? error.message
							: "Failed to load commit graph";
					setState((current) => ({
						...current,
						error: message,
						isInitialLoading: false,
					}));
				} finally {
					loadingPagesRef.current.delete(pageIndex);
				}

				nextPage = pickNextPage();
			}
		} finally {
			runningRef.current = false;
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

	const ensureRangeLoaded = useEffectEvent(
		(startIndex: number, endIndex: number) => {
			if (endIndex < startIndex) {
				return;
			}

			const firstPage = Math.max(
				0,
				Math.floor(startIndex / PAGE_SIZE) - PREFETCH_PAGES,
			);
			const lastPage = Math.max(
				0,
				Math.floor(endIndex / PAGE_SIZE) + PREFETCH_PAGES,
			);
			centerPageRef.current = Math.floor(
				(startIndex + endIndex) / 2 / PAGE_SIZE,
			);

			pendingPagesRef.current = new Set<number>();
			for (let pageIndex = firstPage; pageIndex <= lastPage; pageIndex += 1) {
				const pageStart = pageIndex * PAGE_SIZE;
				const pageEnd = Math.min(
					pageStart + PAGE_SIZE,
					stateRef.current.totalRows || pageStart + PAGE_SIZE,
				);
				const pageHasMissingRows = stateRef.current.rows
					.slice(pageStart, pageEnd)
					.some((row) => row === null);

				if (
					stateRef.current.totalRows === 0 ||
					pageHasMissingRows ||
					pageEnd <= pageStart
				) {
					pendingPagesRef.current.add(pageIndex);
				}
			}

			void runQueue();
		},
	);

	useEffect(() => {
		sessionCache.laneCount = state.laneCount;
		sessionCache.rows = state.rows;
		sessionCache.totalRows = state.totalRows;
	}, [state.laneCount, state.rows, state.totalRows]);

	const reloadGraph = useEffectEvent(() => {
		loadedPagesRef.current = new Set<number>();
		loadingPagesRef.current = new Set<number>();
		pendingPagesRef.current = new Set<number>([0]);
		centerPageRef.current = 0;
		runningRef.current = false;

		setState({
			error: null,
			isInitialLoading: true,
			isPageLoading: false,
			laneCount: 0,
			rows: [],
			totalRows: 0,
		});

		void runQueue();
	});

	// biome-ignore lint/correctness/useExhaustiveDependencies: this is intended
	useEffect(() => {
		if (sessionCache.totalRows > 0) {
			return;
		}
		pendingPagesRef.current = new Set([0]);
		centerPageRef.current = 0;
		void runQueue();
	}, [runQueue]);

	return {
		...state,
		ensureRangeLoaded,
		reloadGraph,
	};
}
