import type { CommitGraphRow } from "@/generated/types";

export type CommitGraphState = {
	currentBranchName: string | null;
	error: string | null;
	isInitialLoading: boolean;
	laneCount: number;
	remotePrefixes: string[];
	remoteRepositoryUrl: string | null;
	rows: Array<CommitGraphRow | null>;
	totalRows: number;
};

type GraphSession = {
	currentBranchName: string | null;
	generation: number;
	hasMore: boolean;
	laneCount: number;
	loadedRowCount: number;
	loading: boolean;
	nextCursor: string | null;
	remotePrefixes: string[];
	remoteRepositoryUrl: string | null;
	repositoryId: string | null;
	requestedEnd: number;
	rows: Array<CommitGraphRow | null>;
	totalRows: number;
};

const MAX_CACHED_REPOSITORIES = 4;
const MAX_CACHED_ROWS = 256;
const CACHED_REFRESH_DELAY_MS = 500;
const cachedViews = new Map<string, CachedGraphView>();
let generation = 0;

export let session: GraphSession = createSession(null);

export function activateCommitGraphSession(repositoryId: string | null) {
	if (session.repositoryId === repositoryId) return;
	cacheCurrentView();
	const cached = repositoryId ? takeCachedView(repositoryId) : undefined;
	session = createSession(repositoryId, cached);
}

export function currentSessionRepositoryId() {
	return session.repositoryId;
}

export function deferCachedCommitGraphRefresh(runLoader: () => void) {
	const repositoryId = session.repositoryId;
	const generation = session.generation;
	session.loading = true;
	const timeout = window.setTimeout(() => {
		if (
			session.repositoryId !== repositoryId ||
			session.generation !== generation
		) {
			return;
		}
		session.loading = false;
		runLoader();
	}, CACHED_REFRESH_DELAY_MS);
	return () => {
		window.clearTimeout(timeout);
		if (
			session.repositoryId === repositoryId &&
			session.generation === generation
		) {
			session.loading = false;
		}
	};
}

export function resetCommitGraphSessionCacheForTests() {
	cachedViews.clear();
	session = createSession(null);
}

function cacheCurrentView() {
	if (!session.repositoryId || session.rows.length === 0) return;
	cachedViews.delete(session.repositoryId);
	cachedViews.set(session.repositoryId, {
		currentBranchName: session.currentBranchName,
		laneCount: session.laneCount,
		remotePrefixes: session.remotePrefixes,
		remoteRepositoryUrl: session.remoteRepositoryUrl,
		rows: session.rows.slice(0, MAX_CACHED_ROWS),
		totalRows: session.totalRows,
	});
	while (cachedViews.size > MAX_CACHED_REPOSITORIES) {
		const oldest = cachedViews.keys().next().value;
		if (oldest === undefined) break;
		cachedViews.delete(oldest);
	}
}

function takeCachedView(repositoryId: string) {
	const cached = cachedViews.get(repositoryId);
	if (!cached) return undefined;
	cachedViews.delete(repositoryId);
	return cached;
}

function createSession(
	repositoryId: string | null,
	cached?: CachedGraphView,
): GraphSession {
	return {
		currentBranchName: cached?.currentBranchName ?? null,
		generation: ++generation,
		hasMore: true,
		laneCount: cached?.laneCount ?? 0,
		loadedRowCount: 0,
		loading: false,
		nextCursor: null,
		remotePrefixes: cached?.remotePrefixes ?? [],
		remoteRepositoryUrl: cached?.remoteRepositoryUrl ?? null,
		repositoryId,
		requestedEnd: 0,
		rows: cached?.rows ?? [],
		totalRows: cached?.totalRows ?? 0,
	};
}

type CachedGraphView = Pick<
	GraphSession,
	| "currentBranchName"
	| "laneCount"
	| "remotePrefixes"
	| "remoteRepositoryUrl"
	| "rows"
	| "totalRows"
>;
