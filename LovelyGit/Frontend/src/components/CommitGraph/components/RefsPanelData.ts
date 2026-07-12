import type {
	CommitGraphRow,
	CommitRefInfo,
	CommitRefKind,
	RepositoryRefItem,
} from "@/generated/types";
import { normalizeRefs, refLabelForRemotes } from "./RefCellUtils";

export type RefPanelItem = {
	commitHash: string;
	isCurrent: boolean;
	kind: CommitRefKind;
	label: string;
	name: string;
	remoteUrl: string | null;
	row: CommitGraphRow | null;
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
	refs,
	remotePrefixes,
	rows,
}: {
	currentBranchName: string | null;
	refs?: RepositoryRefItem[] | null;
	remotePrefixes: string[];
	rows: Array<CommitGraphRow | null>;
}): RefPanelSection[] {
	const itemsByKey = new Map<string, RefPanelItem>();
	const rowsByHash = new Map(
		rows.flatMap((row) => (row ? [[row.commit.hash, row] as const] : [])),
	);

	for (const ref of refs ?? []) {
		addRefItem(itemsByKey, {
			commitHash: ref.commitHash,
			currentBranchName,
			ref,
			remotePrefixes,
			row: rowsByHash.get(ref.commitHash) ?? null,
		});
	}

	for (const row of rows) {
		if (!row) {
			continue;
		}

		for (const ref of rowRefs(row, remotePrefixes)) {
			const key = refKey(ref.kind, ref.name, row.commit.hash);
			const existing = itemsByKey.get(key);
			if (existing) {
				existing.row ??= row;
				continue;
			}

			addRefItem(itemsByKey, {
				commitHash: row.commit.hash,
				currentBranchName,
				ref: {
					commitHash: row.commit.hash,
					kind: ref.kind,
					name: ref.name,
					remoteUrl: ref.remoteUrl,
				},
				remotePrefixes,
				row,
			});
		}
	}

	const byKind = groupItemsByKind(itemsByKey.values());
	return sectionOrder
		.map((kind) => ({
			count: byKind.get(kind)?.length ?? 0,
			items: sortItems(byKind.get(kind) ?? []),
			kind,
			label: sectionLabels[kind],
		}))
		.filter((section) => section.count > 0);
}

export function filterRefPanelSections(
	sections: RefPanelSection[],
	query: string,
): RefPanelSection[] {
	const normalizedQuery = query.trim().toLocaleLowerCase();
	if (normalizedQuery.length === 0) {
		return sections;
	}

	return sections
		.map((section) => {
			const items = section.items.filter((item) =>
				`${item.name} ${item.label} ${item.commitHash}`
					.toLocaleLowerCase()
					.includes(normalizedQuery),
			);
			return {
				...section,
				count: items.length,
				items,
			};
		})
		.filter((section) => section.count > 0);
}

export function refPanelItemToRefInfo(item: RefPanelItem): CommitRefInfo {
	return {
		kind: item.kind,
		name: item.name,
		remoteUrl: item.remoteUrl,
	};
}

function addRefItem(
	items: Map<string, RefPanelItem>,
	input: {
		commitHash: string;
		currentBranchName: string | null;
		ref: RepositoryRefItem;
		remotePrefixes: string[];
		row: CommitGraphRow | null;
	},
) {
	const key = refKey(input.ref.kind, input.ref.name, input.commitHash);
	if (items.has(key)) {
		return;
	}

	items.set(key, {
		commitHash: input.commitHash,
		isCurrent:
			input.ref.kind === "Local" && input.ref.name === input.currentBranchName,
		kind: input.ref.kind,
		label: refLabelForRemotes(input.ref.name, input.remotePrefixes),
		name: input.ref.name,
		remoteUrl: input.ref.remoteUrl,
		row: input.row,
	});
}

function refKey(kind: CommitRefKind, name: string, commitHash: string) {
	return `${kind}:${name}:${commitHash}`;
}

function groupItemsByKind(items: Iterable<RefPanelItem>) {
	const byKind = new Map<CommitRefKind, RefPanelItem[]>();
	for (const item of items) {
		const sectionItems = byKind.get(item.kind) ?? [];
		sectionItems.push(item);
		byKind.set(item.kind, sectionItems);
	}

	return byKind;
}

function rowRefs(
	row: CommitGraphRow,
	remotePrefixes: string[],
): CommitRefInfo[] {
	return normalizeRefs(row.commit.refs, remotePrefixes);
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
