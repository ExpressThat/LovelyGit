import { Cloud, HardDrive, Tag } from "lucide-react";
import {
	Tooltip,
	TooltipContent,
	TooltipTrigger,
} from "@/components/ui/tooltip";
import type {
	CommitGraphRow,
	CommitRefInfo,
	CommitRefKind,
} from "@/generated/types";
import { refLabel } from "../utils/format";

export function RefCell({
	currentBranchName,
	remotePrefixes,
	row,
}: {
	currentBranchName: string | null;
	remotePrefixes: string[];
	row: CommitGraphRow;
}) {
	const refs =
		row.commit.refs.length > 0
			? normalizeRefs(row.commit.refs, row.commit.tags, remotePrefixes)
			: buildLegacyRefs(row.commit.branches, row.commit.tags, remotePrefixes);
	const groups = groupRefs(refs, remotePrefixes, currentBranchName);

	if (groups.length === 0) {
		return <div className="h-[17px]" />;
	}

	const primaryGroup = groups[0];
	return (
		<div className="flex min-w-0 gap-1">
			<RefGroupPill
				group={primaryGroup}
				hiddenGroups={groups.slice(1)}
				remotePrefixes={remotePrefixes}
			/>
		</div>
	);
}

function normalizeRefs(
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

	return ref;
}

type RefGroup = {
	icons: CommitRefKind[];
	key: string;
	primary: CommitRefInfo;
	refs: CommitRefInfo[];
};

function RefGroupPill({
	group,
	hiddenGroups,
	remotePrefixes,
}: {
	group: RefGroup;
	hiddenGroups: RefGroup[];
	remotePrefixes: string[];
}) {
	const hiddenCount = hiddenGroups.length;
	const icons = uniqueKinds(group.refs);
	const pill = (
		<span className="inline-flex h-[17px] max-w-full items-center gap-1 overflow-hidden whitespace-nowrap rounded-[3px] border border-border bg-secondary px-1 text-[11px] text-secondary-foreground shadow-sm">
			<span className="inline-flex shrink-0 items-center gap-0.5">
				{icons.map((kind) => (
					<RefIcon kind={kind} key={kind} />
				))}
			</span>
			<span className="min-w-0 flex-1 truncate">
				{refLabelForRemotes(group.primary.name, remotePrefixes)}
			</span>
			{hiddenCount > 0 ? (
				<span className="shrink-0 font-semibold text-muted-foreground">
					+{hiddenCount}
				</span>
			) : null}
		</span>
	);

	if (hiddenGroups.length === 0) {
		return pill;
	}

	return (
		<Tooltip>
			<TooltipTrigger
				closeDelay={100}
				delay={150}
				render={<span className="inline-flex min-w-0 max-w-full" />}
			>
				{pill}
			</TooltipTrigger>
			<TooltipContent
				align="start"
				className="flex min-w-max flex-col gap-1 p-1"
				side="bottom"
				sideOffset={4}
			>
				{hiddenGroups.map((refGroup) => (
					<RefListGroupPill
						group={refGroup}
						key={refGroup.key}
						remotePrefixes={remotePrefixes}
					/>
				))}
			</TooltipContent>
		</Tooltip>
	);
}

function RefListGroupPill({
	group,
	remotePrefixes,
}: {
	group: RefGroup;
	remotePrefixes: string[];
}) {
	const icons = uniqueKinds(group.refs);
	return (
		<span className="inline-flex h-6 max-w-[420px] items-center gap-1 rounded-sm border border-border bg-secondary px-2 text-xs text-secondary-foreground shadow-sm">
			<span className="inline-flex shrink-0 items-center gap-0.5">
				{icons.map((kind) => (
					<RefIcon kind={kind} key={kind} />
				))}
			</span>
			<span className="truncate">
				{refLabelForRemotes(group.primary.name, remotePrefixes)}
			</span>
		</span>
	);
}

function buildLegacyRefs(
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
		})),
		...tags.map((name) => ({ kind: "Tag" as const, name })),
	];
}

function groupRefs(
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
	const label = refLabelForRemotes(ref.name, remotePrefixes);
	return ref.kind === "Tag" ? `tag:${label}` : `branch:${label}`;
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

function uniqueKinds(refs: CommitRefInfo[]) {
	return [...new Set(refs.map((ref) => ref.kind))].sort(
		(left, right) => kindRank(left) - kindRank(right),
	);
}

function kindRank(kind: CommitRefKind) {
	return kind === "Local" ? 0 : kind === "Remote" ? 1 : 2;
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
	return remotePrefixes.some(
		(prefix) => name.startsWith(`${prefix}/`),
	);
}

function refLabelForRemotes(name: string, remotePrefixes: string[]) {
	for (const prefix of remotePrefixes) {
		if (name.startsWith(`${prefix}/`)) {
			return name.slice(prefix.length + 1);
		}
	}

	return refLabel(name);
}

function RefIcon({ kind }: { kind: CommitRefKind }) {
	if (kind === "Remote") {
		return <Cloud aria-hidden="true" className="text-sky-400" size={11} />;
	}

	if (kind === "Tag") {
		return <Tag aria-hidden="true" className="text-amber-400" size={11} />;
	}

	return <HardDrive aria-hidden="true" className="text-emerald-400" size={11} />;
}
