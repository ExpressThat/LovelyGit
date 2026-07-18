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
			setState({ ...emptyState(repositoryId), anchorIndex: index });
			onOpen(row);
			return;
		}

		setState((previous) => {
			const active =
				previous.repositoryId === repositoryId
					? previous
					: emptyState(repositoryId);
			if (gesture.shiftKey && active.anchorIndex != null) {
				const selected = selectRange(
					rows,
					active.anchorIndex,
					index,
					gesture.ctrlKey || gesture.metaKey
						? active
						: emptyState(repositoryId),
				);
				return {
					repositoryId,
					anchorIndex: active.anchorIndex,
					...selected,
				};
			}

			const hashes = new Set(active.hashes);
			const selectedRows = new Map(active.selectedRows);
			const hash = row.commit.hash;
			if (hashes.has(hash)) {
				hashes.delete(hash);
				selectedRows.delete(hash);
			} else if (hashes.size < MAX_SELECTED_COMMITS) {
				hashes.add(hash);
				selectedRows.set(hash, { index, row });
			}
			return { repositoryId, anchorIndex: index, hashes, selectedRows };
		});
	};
	const rowsFor = (clicked: CommitGraphRow, mode: "cherry-pick" | "revert") =>
		orderSelectedCommits(
			current.hashes.has(clicked.commit.hash)
				? current.selectedRows.values()
				: [{ index: -1, row: clicked }],
			mode,
		);

	return {
		clear: () => setState(emptyState(repositoryId)),
		comparison: () => comparisonPair(current.selectedRows.values()),
		count: current.hashes.size,
		hashes: current.hashes,
		ordered: (mode: "cherry-pick" | "revert") =>
			orderSelectedCommits(current.selectedRows.values(), mode),
		rowsFor,
		select,
	};
}

export function comparisonPair(selectedRows: Iterable<SelectedCommit>) {
	const selected = [...selectedRows].sort(byGraphIndex);
	if (selected.length !== 2) return null;
	return { base: selected[1].row, target: selected[0].row };
}

export function orderSelectedCommits(
	selectedRows: Iterable<SelectedCommit>,
	mode: "cherry-pick" | "revert",
) {
	const selected = [...selectedRows].sort(byGraphIndex);
	if (mode === "cherry-pick") selected.reverse();
	return selected.map((item) => item.row);
}

function selectRange(
	rows: Array<CommitGraphRow | null>,
	anchorIndex: number,
	targetIndex: number,
	existing: Pick<SelectionState, "hashes" | "selectedRows">,
) {
	const hashes = new Set(existing.hashes);
	const selectedRows = new Map(existing.selectedRows);
	const step = targetIndex >= anchorIndex ? 1 : -1;
	for (let index = anchorIndex; ; index += step) {
		const row = rows[index];
		if (
			row &&
			(hashes.has(row.commit.hash) || hashes.size < MAX_SELECTED_COMMITS)
		) {
			hashes.add(row.commit.hash);
			selectedRows.set(row.commit.hash, { index, row });
		}
		if (index === targetIndex || hashes.size >= MAX_SELECTED_COMMITS) break;
	}
	return { hashes, selectedRows };
}

function emptyState(repositoryId: string | null): SelectionState {
	return {
		repositoryId,
		anchorIndex: null,
		hashes: new Set(),
		selectedRows: new Map(),
	};
}

function byGraphIndex(left: SelectedCommit, right: SelectedCommit) {
	return left.index - right.index;
}

export type SelectedCommit = { index: number; row: CommitGraphRow };

type SelectionState = {
	repositoryId: string | null;
	anchorIndex: number | null;
	hashes: Set<string>;
	selectedRows: Map<string, SelectedCommit>;
};
