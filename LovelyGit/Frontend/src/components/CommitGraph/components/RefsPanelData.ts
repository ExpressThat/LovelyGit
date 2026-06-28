import type {
	CommitGraphRow,
	CommitRefInfo,
	CommitRefKind,
} from "@/generated/types";
import {
	buildLegacyRefs,
	normalizeRefs,
	refLabelForRemotes,
} from "./RefCellUtils";

export type RefPanelItem = {
	commitHash: string;
	isCurrent: boolean;
	kind: CommitRefKind;
	label: string;
	name: string;
	row: CommitGraphRow;
};

export type RefPanelSection = {
	count: number;
	items: RefPanelItem[];
	kind: CommitRefKind;
	label: string;
};

const sectionLabels: Record<CommitRefKind, string> = {
	Local: "Branches",
	Remote: "Remote Branches",
	Stash: "Stashes",
	Tag: "Tags",
};

const sectionOrder: CommitRefKind[] = ["Local", "Remote", "Tag", "Stash"];

export function buildRefPanelSections({
	currentBranchName,
	remotePrefixes,
	rows,
}: {
	currentBranchName: string | null;
	remotePrefixes: string[];
	rows: Array<CommitGraphRow | null>;
}): RefPanelSection[] {
	const byKind = new Map<CommitRefKind, RefPanelItem[]>();
	const seen = new Set<string>();

	for (const row of rows) {
		if (!row) {
			continue;
		}

		for (const ref of rowRefs(row, remotePrefixes)) {
			const label = refLabelForRemotes(ref.name, remotePrefixes);
			const key = `${ref.kind}:${ref.name}:${row.commit.hash}`;
			if (seen.has(key)) {
				continue;
			}

			seen.add(key);
			const items = byKind.get(ref.kind) ?? [];
			items.push({
				commitHash: row.commit.hash,
				isCurrent: ref.kind === "Local" && ref.name === currentBranchName,
				kind: ref.kind,
				label,
				name: ref.name,
				row,
			});
			byKind.set(ref.kind, items);
		}
	}

	return sectionOrder
		.map((kind) => ({
			count: byKind.get(kind)?.length ?? 0,
			items: sortItems(byKind.get(kind) ?? []),
			kind,
			label: sectionLabels[kind],
		}))
		.filter((section) => section.count > 0);
}

function rowRefs(
	row: CommitGraphRow,
	remotePrefixes: string[],
): CommitRefInfo[] {
	return row.commit.refs.length > 0
		? normalizeRefs(row.commit.refs, row.commit.tags, remotePrefixes)
		: buildLegacyRefs(row.commit.branches, row.commit.tags, remotePrefixes);
}

function sortItems(items: RefPanelItem[]) {
	return items
		.slice()
		.sort((left, right) =>
			left.isCurrent === right.isCurrent
				? left.label.localeCompare(right.label)
				: left.isCurrent
					? -1
					: 1,
		);
}
