import type { CommitGraphRow } from "@/generated/types";
import { MAX_CACHED_REPOSITORIES } from "@/lib/repositoryCacheLimits";

export type CommitGraphState = {
	currentBranchName: string | null;
	error: string | null;
	isInitialLoading: boolean;
	laneCount: number;
	remotePrefixes: string[];
	remoteRepositoryUrl: string | null;
	refRowsByHash: ReadonlyMap<string, CommitGraphRow>;
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
	refRowsByHash: Map<string, CommitGraphRow>;
	repositoryId: string | null;
	requestedEnd: number;
	rows: Array<CommitGraphRow | null>;
	totalRows: number;
};

export const MAX_CACHED_ROWS = 64;
export const CACHED_REFRESH_DELAY_MS = 1_500;
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

export function updateLocalBranchRefInSession(
	oldName: string,
	newName: string | null,
) {
	let row: CommitGraphRow | undefined;
	for (const candidate of session.refRowsByHash.values()) {
		if (
			candidate.commit.refs.some(
				(ref) => ref.kind === "Local" && ref.name === oldName,
			)
		) {
			row = candidate;
			break;
		}
	}
	if (!row) return false;
	const refs = newName
		? row.commit.refs.map((ref) =>
				ref.kind === "Local" && ref.name === oldName
					? { ...ref, name: newName }
					: ref,
			)
		: row.commit.refs.filter(
				(ref) => ref.kind !== "Local" || ref.name !== oldName,
			);
	const nextRow = {
		...row,
		commit: { ...row.commit, refs },
		isBranchTip: refs.some((ref) =>
			["Local", "Remote", "Stash"].includes(ref.kind),
		),
	};
	const rows = session.rows.slice();
	rows[row.rowIndex] = nextRow;
	const refRowsByHash = new Map(session.refRowsByHash);
	if (refs.length > 0) refRowsByHash.set(row.commit.hash, nextRow);
	else refRowsByHash.delete(row.commit.hash);
	session.rows = rows;
	session.refRowsByHash = refRowsByHash;
	if (session.currentBranchName === oldName)
		session.currentBranchName = newName;
	return true;
}

export function deferCachedCommitGraphRefresh(runLoader: () => void) {
	const repositoryId = session.repositoryId;
	const generation = session.generation;
	session.loading = true;
	const timeout = globalThis.setTimeout(() => {
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
		globalThis.clearTimeout(timeout);
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
		refRowsByHash: indexRefRows(cached?.rows ?? []),
		repositoryId,
		requestedEnd: 0,
		rows: cached?.rows ?? [],
		totalRows: cached?.totalRows ?? 0,
	};
}

function indexRefRows(rows: Array<CommitGraphRow | null>) {
	const indexed = new Map<string, CommitGraphRow>();
	for (const row of rows) {
		if (row && row.commit.refs.length > 0) indexed.set(row.commit.hash, row);
	}
	return indexed;
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
