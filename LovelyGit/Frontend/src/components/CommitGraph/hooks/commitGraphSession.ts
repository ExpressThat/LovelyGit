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

export const session: GraphSession = {
	currentBranchName: null,
	generation: 0,
	hasMore: true,
	laneCount: 0,
	loadedRowCount: 0,
	loading: false,
	nextCursor: null,
	remotePrefixes: [],
	remoteRepositoryUrl: null,
	repositoryId: null,
	requestedEnd: 0,
	rows: [],
	totalRows: 0,
};

export function currentSessionRepositoryId() {
	return session.repositoryId;
}
