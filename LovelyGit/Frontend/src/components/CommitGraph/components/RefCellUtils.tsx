import { Archive, Cloud, HardDrive, Tag } from "lucide-react";
import type { CommitRefInfo, CommitRefKind } from "@/generated/types";
import { refLabel } from "../utils/format";

export function normalizeRefs(
	refs: CommitRefInfo[],
	_tags: string[],
	remotePrefixes: string[],
): CommitRefInfo[] {
	const seenLocalLabels = new Set<string>();
	return refs.map((rawRef) => {
		const ref = normalizeRuntimeKind(rawRef);
		if (ref.kind !== "Local") {
			return ref;
		}

		if (isLikelyRemoteBranch(ref.name, remotePrefixes)) {
			return { ...ref, kind: "Remote" };
		}

		const label = refLabelForRemotes(ref.name, remotePrefixes);
		if (seenLocalLabels.has(label)) {
			return { ...ref, kind: "Remote" };
		}

		seenLocalLabels.add(label);
		return ref;
	});
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
export function buildLegacyRefs(
	branches: string[],
	tags: string[],
	remotePrefixes: string[],
): CommitRefInfo[] {
	const localBranchNames = new Set(
		branches.filter((branch) => !isLikelyRemoteBranch(branch, remotePrefixes)),
	);
	return [
		...branches.map((name) => ({
			kind: inferLegacyBranchKind(name, localBranchNames, remotePrefixes),
			name,
			remoteUrl: null,
		})),
		...tags.map((name) => ({ kind: "Tag" as const, name, remoteUrl: null })),
	];
}

export function groupRefs(
	refs: CommitRefInfo[],
	remotePrefixes: string[],
	currentBranchName: string | null,
): RefGroup[] {
	const groups = new Map<string, CommitRefInfo[]>();
	for (const ref of refs) {
		const key = displayKey(ref, remotePrefixes);
		const group = groups.get(key);
		if (group) {
			group.push(ref);
		} else {
			groups.set(key, [ref]);
		}
	}

	return [...groups.entries()]
		.map(([key, groupRefsForKey]) => {
			const ordered = normalizeGroupedRefs(groupRefsForKey)
				.slice()
				.sort((left, right) => compareRefs(left, right, remotePrefixes));
			return {
				icons: uniqueKinds(ordered),
				key,
				primary: ordered[0],
				refs: ordered,
			};
		})
		.sort((left, right) =>
			compareGroups(left, right, remotePrefixes, currentBranchName),
		);
}

function compareGroups(
	left: RefGroup,
	right: RefGroup,
	remotePrefixes: string[],
	currentBranchName: string | null,
) {
	const leftIsCurrent = groupContainsCurrentBranch(left, currentBranchName);
	const rightIsCurrent = groupContainsCurrentBranch(right, currentBranchName);
	if (leftIsCurrent !== rightIsCurrent) {
		return leftIsCurrent ? -1 : 1;
	}

	return compareRefs(left.primary, right.primary, remotePrefixes);
}

function groupContainsCurrentBranch(
	group: RefGroup,
	currentBranchName: string | null,
) {
	return currentBranchName != null
		? group.refs.some(
				(ref) => ref.kind === "Local" && ref.name === currentBranchName,
			)
		: false;
}

function normalizeGroupedRefs(refs: CommitRefInfo[]): CommitRefInfo[] {
	let sawLocal = false;
	return refs.map((ref) => {
		if (ref.kind !== "Local") {
			return ref;
		}

		if (sawLocal) {
			return { ...ref, kind: "Remote" };
		}

		sawLocal = true;
		return ref;
	});
}

function displayKey(ref: CommitRefInfo, remotePrefixes: string[]) {
	if (ref.kind === "Stash") {
		return "stash";
	}

	const label = refLabelForRemotes(ref.name, remotePrefixes);
	if (ref.kind === "Tag") {
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
	return [...new Set(refs.map((ref) => ref.kind))].sort(
		(left, right) => kindRank(left) - kindRank(right),
	);
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

function inferLegacyBranchKind(
	name: string,
	localBranchNames: Set<string>,
	remotePrefixes: string[],
): CommitRefKind {
	if (!isLikelyRemoteBranch(name, remotePrefixes)) {
		return "Local";
	}

	const slashIndex = name.indexOf("/");
	const branchName = slashIndex >= 0 ? name.slice(slashIndex + 1) : name;
	return localBranchNames.has(branchName) ? "Remote" : "Local";
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
