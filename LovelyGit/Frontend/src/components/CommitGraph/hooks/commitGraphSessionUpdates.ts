import type { CommitGraphResponse } from "@/generated/types";
import { session } from "./commitGraphSession";
import { compactCommitGraphRow } from "./compactCommitGraphRow";

export function applyCommitGraphResponse(
	response: CommitGraphResponse,
	requiredLength: number,
	pageSize: number,
) {
	const nextRows = session.rows;
	let nextRefRows = session.refRowsByHash;
	let copiedRefRows = false;
	let loadedRowCount = session.loadedRowCount;
	for (const row of response.rows) {
		const compactRow = compactCommitGraphRow(row);
		nextRows[row.rowIndex] = compactRow;
		const updatesRefIndex =
			compactRow.commit.refs.length > 0 ||
			nextRefRows.has(compactRow.commit.hash);
		if (updatesRefIndex) {
			if (!copiedRefRows) {
				nextRefRows = new Map(nextRefRows);
				copiedRefRows = true;
			}
			if (compactRow.commit.refs.length > 0) {
				nextRefRows.set(compactRow.commit.hash, compactRow);
			} else {
				nextRefRows.delete(compactRow.commit.hash);
			}
		}
		loadedRowCount = Math.max(loadedRowCount, row.rowIndex + 1);
	}
	if (!response.hasMore) nextRows.length = response.totalRows;
	session.nextCursor = response.nextCursor;
	session.hasMore = response.hasMore;
	session.currentBranchName = response.currentBranchName ?? null;
	session.remotePrefixes = response.remotePrefixes;
	session.remoteRepositoryUrl = response.remoteRepositoryUrl;
	session.refRowsByHash = nextRefRows;
	session.rows = nextRows;
	session.loadedRowCount = loadedRowCount;
	session.laneCount = Math.max(session.laneCount, response.laneCount);
	session.totalRows = response.hasMore
		? Math.max(session.totalRows, nextRows.length + pageSize, requiredLength)
		: response.totalRows;
}
