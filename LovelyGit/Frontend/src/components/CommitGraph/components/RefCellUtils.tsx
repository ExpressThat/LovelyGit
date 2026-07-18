import { Archive, Cloud, HardDrive, Tag } from "@/components/icons/lovelyIcons";
import type { CommitRefInfo, CommitRefKind } from "@/generated/types";
import { refLabel } from "../utils/format";

export function normalizeRefs(
	refs: CommitRefInfo[],
	remotePrefixes: string[],
): CommitRefInfo[] {
	const seenLocalLabels = new Set<string>();
	let normalized: CommitRefInfo[] | null = null;
	for (let index = 0; index < refs.length; index++) {
		const rawRef = refs[index];
		let ref = normalizeRuntimeKind(rawRef);
		if (ref.kind !== "Local") {
			// Already normalized.
		} else {
			const label = refLabelForRemotes(ref.name, remotePrefixes);
			if (
				isLikelyRemoteBranch(ref.name, remotePrefixes) ||
				seenLocalLabels.has(label)
			) {
				ref = { ...ref, kind: "Remote" };
			} else {
				seenLocalLabels.add(label);
			}
		}

		if (ref !== rawRef && normalized === null) {
			normalized = refs.slice(0, index);
		}
		normalized?.push(ref);
	}
	return normalized ?? refs;
}

function normalizeRuntimeKind(ref: CommitRefInfo): CommitRefInfo {
	const kind = ref.kind as CommitRefKind | number;
	if (kind === 0) {
		return { ...ref, kind: "Local" };
	}

	if (kind === 1) {
		return { ...ref, kind: "Remote" };
	}

	if (kind === 2) {
		return { ...ref, kind: "Tag" };
	}

	if (kind === 3) {
		return { ...ref, kind: "Stash" };
	}

	return ref;
}

export type RefGroup = {
	icons: CommitRefKind[];
	key: string;
	primary: CommitRefInfo;
	refs: CommitRefInfo[];
};
export function groupRefs(
	refs: CommitRefInfo[],
	remotePrefixes: string[],
	currentBranchName: string | null,
): RefGroup[] {
	const groups = new Map<string, RefGroupBuilder>();
	for (const sourceRef of refs) {
		const label = refLabelForRemotes(sourceRef.name, remotePrefixes);
		const key = displayKey(sourceRef.kind, label);
		const group = groups.get(key);
		if (group) {
			const ref =
				sourceRef.kind === "Local" && group.hasLocal
					? { ...sourceRef, kind: "Remote" as const }
					: sourceRef;
			group.hasLocal ||= ref.kind === "Local";
			group.isCurrent ||=
				ref.kind === "Local" && ref.name === currentBranchName;
			group.kindMask |= 1 << kindRank(ref.kind);
			group.refs.push(ref);
		} else {
			groups.set(key, createGroup(sourceRef, key, label, currentBranchName));
		}
	}

	const orderedGroups = [...groups.values()];
	for (const group of orderedGroups) {
		group.refs.sort((left, right) => compareRefs(left, right, remotePrefixes));
	}
	orderedGroups.sort((left, right) => {
		if (left.isCurrent !== right.isCurrent) return left.isCurrent ? -1 : 1;
		const rank = kindRank(left.refs[0].kind) - kindRank(right.refs[0].kind);
		return rank || left.label.localeCompare(right.label);
	});
	return orderedGroups.map((group) => ({
		icons: kindsFromMask(group.kindMask),
		key: group.key,
		primary: group.refs[0],
		refs: group.refs,
	}));
}

function createGroup(
	ref: CommitRefInfo,
	key: string,
	label: string,
	currentBranchName: string | null,
): RefGroupBuilder {
	return {
		hasLocal: ref.kind === "Local",
		isCurrent: ref.kind === "Local" && ref.name === currentBranchName,
		key,
		kindMask: 1 << kindRank(ref.kind),
		label,
		refs: [ref],
	};
}

type RefGroupBuilder = {
	hasLocal: boolean;
	isCurrent: boolean;
	key: string;
	kindMask: number;
	label: string;
	refs: CommitRefInfo[];
};

function displayKey(kind: CommitRefKind, label: string) {
	if (kind === "Stash") {
		return "stash";
	}

	if (kind === "Tag") {
		return `tag:${label}`;
	}

	return `branch:${label}`;
}

function compareRefs(
	left: CommitRefInfo,
	right: CommitRefInfo,
	remotePrefixes: string[],
) {
	const kindCompare = kindRank(left.kind) - kindRank(right.kind);
	return kindCompare !== 0
		? kindCompare
		: refLabelForRemotes(left.name, remotePrefixes).localeCompare(
				refLabelForRemotes(right.name, remotePrefixes),
			);
}

export function uniqueKinds(refs: CommitRefInfo[]) {
	let mask = 0;
	for (const ref of refs) mask |= 1 << kindRank(ref.kind);
	return kindsFromMask(mask);
}

function kindsFromMask(mask: number): CommitRefKind[] {
	const kinds: CommitRefKind[] = [];
	if (mask & 1) kinds.push("Local");
	if (mask & 2) kinds.push("Remote");
	if (mask & 4) kinds.push("Tag");
	if (mask & 8) kinds.push("Stash");
	return kinds;
}

function kindRank(kind: CommitRefKind) {
	if (kind === "Local") {
		return 0;
	}

	if (kind === "Remote") {
		return 1;
	}

	return kind === "Tag" ? 2 : 3;
}

function isLikelyRemoteBranch(name: string, remotePrefixes: string[]) {
	return remotePrefixes.some((prefix) => name.startsWith(`${prefix}/`));
}

export function refLabelForRemotes(name: string, remotePrefixes: string[]) {
	if (name === "stash" || name.startsWith("stash@{")) {
		return "stash";
	}

	for (const prefix of remotePrefixes) {
		if (name.startsWith(`${prefix}/`)) {
			return name.slice(prefix.length + 1);
		}
	}

	return refLabel(name);
}

export function RefIcon({ kind }: { kind: CommitRefKind }) {
	if (kind === "Remote") {
		return <Cloud aria-hidden="true" className="text-sky-400" size={11} />;
	}

	if (kind === "Tag") {
		return <Tag aria-hidden="true" className="text-amber-400" size={11} />;
	}

	if (kind === "Stash") {
		return <Archive aria-hidden="true" className="text-violet-400" size={11} />;
	}

	return (
		<HardDrive aria-hidden="true" className="text-emerald-400" size={11} />
	);
}
