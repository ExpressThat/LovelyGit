import { useState } from "react";
import type { CommitGraphRow } from "@/generated/types";

const MAX_SELECTED_COMMITS = 100;

export type SelectionGesture = {
	ctrlKey: boolean;
	metaKey: boolean;
	shiftKey: boolean;
};

export function useCommitMultiSelection(
	repositoryId: string | null,
	rows: Array<CommitGraphRow | null>,
) {
	const [state, setState] = useState<SelectionState>(() =>
		emptyState(repositoryId),
	);
	const current =
		state.repositoryId === repositoryId ? state : emptyState(repositoryId);
	const select = (
		row: CommitGraphRow,
		index: number,
		gesture: SelectionGesture,
		onOpen: (row: CommitGraphRow) => void,
	) => {
		if (!gesture.ctrlKey && !gesture.metaKey && !gesture.shiftKey) {
			setState({ repositoryId, anchorIndex: index, hashes: new Set() });
			onOpen(row);
			return;
		}

		setState((previous) => {
			const active =
				previous.repositoryId === repositoryId
					? previous
					: emptyState(repositoryId);
			if (gesture.shiftKey && active.anchorIndex != null) {
				return {
					repositoryId,
					anchorIndex: active.anchorIndex,
					hashes: selectRange(
						rows,
						active.anchorIndex,
						index,
						gesture.ctrlKey || gesture.metaKey ? active.hashes : new Set(),
					),
				};
			}

			const hashes = new Set(active.hashes);
			const hash = row.commit.hash;
			if (hashes.has(hash)) hashes.delete(hash);
			else if (hashes.size < MAX_SELECTED_COMMITS) hashes.add(hash);
			return { repositoryId, anchorIndex: index, hashes };
		});
	};
	const rowsFor = (clicked: CommitGraphRow, mode: "cherry-pick" | "revert") =>
		orderSelectedCommits(
			rows,
			current.hashes.has(clicked.commit.hash)
				? current.hashes
				: new Set([clicked.commit.hash]),
			mode,
		);

	return {
		clear: () => setState(emptyState(repositoryId)),
		count: current.hashes.size,
		hashes: current.hashes,
		ordered: (mode: "cherry-pick" | "revert") =>
			orderSelectedCommits(rows, current.hashes, mode),
		rowsFor,
		select,
	};
}

export function orderSelectedCommits(
	rows: Array<CommitGraphRow | null>,
	hashes: ReadonlySet<string>,
	mode: "cherry-pick" | "revert",
) {
	const selected = rows.filter(
		(row): row is CommitGraphRow => row != null && hashes.has(row.commit.hash),
	);
	return mode === "cherry-pick" ? selected.reverse() : selected;
}

function selectRange(
	rows: Array<CommitGraphRow | null>,
	anchorIndex: number,
	targetIndex: number,
	existing: ReadonlySet<string>,
) {
	const hashes = new Set(existing);
	const step = targetIndex >= anchorIndex ? 1 : -1;
	for (let index = anchorIndex; ; index += step) {
		const row = rows[index];
		if (row) hashes.add(row.commit.hash);
		if (index === targetIndex || hashes.size >= MAX_SELECTED_COMMITS) break;
	}
	return hashes;
}

function emptyState(repositoryId: string | null): SelectionState {
	return { repositoryId, anchorIndex: null, hashes: new Set() };
}

type SelectionState = {
	repositoryId: string | null;
	anchorIndex: number | null;
	hashes: Set<string>;
};
