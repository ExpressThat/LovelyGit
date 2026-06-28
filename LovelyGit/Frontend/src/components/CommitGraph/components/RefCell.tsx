import type { CommitGraphRow } from "@/generated/types";
import { BranchRefContextMenu } from "./BranchRefContextMenu";
import {
	buildLegacyRefs,
	groupRefs,
	normalizeRefs,
	type RefGroup,
	RefIcon,
	refLabelForRemotes,
	uniqueKinds,
} from "./RefCellUtils";

export function RefCell({
	currentBranchName,
	onRefsChanged,
	remotePrefixes,
	repositoryId,
	row,
}: {
	currentBranchName: string | null;
	onRefsChanged: () => void;
	remotePrefixes: string[];
	repositoryId: string | null;
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

	return (
		<div className="flex min-w-0 gap-1 overflow-hidden">
			{groups.map((group) => (
				<RefGroupPill
					currentBranchName={currentBranchName}
					group={group}
					key={group.key}
					onRefsChanged={onRefsChanged}
					remotePrefixes={remotePrefixes}
					repositoryId={repositoryId}
				/>
			))}
		</div>
	);
}
function RefGroupPill({
	currentBranchName,
	group,
	onRefsChanged,
	remotePrefixes,
	repositoryId,
}: {
	currentBranchName: string | null;
	group: RefGroup;
	onRefsChanged: () => void;
	remotePrefixes: string[];
	repositoryId: string | null;
}) {
	const icons = uniqueKinds(group.refs);
	const pill = (
		<span
			className="inline-flex h-[17px] max-w-[132px] shrink-0 items-center gap-1 overflow-hidden whitespace-nowrap rounded-[3px] border border-border bg-secondary px-1 text-[11px] text-secondary-foreground shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
			title={group.primary.name}
		>
			<span className="inline-flex shrink-0 items-center gap-0.5">
				{icons.map((kind) => (
					<RefIcon kind={kind} key={kind} />
				))}
			</span>
			<span className="min-w-0 flex-1 truncate">
				{refLabelForRemotes(group.primary.name, remotePrefixes)}
			</span>
		</span>
	);
	return (
		<BranchRefContextMenu
			currentBranchName={currentBranchName}
			onRefsChanged={onRefsChanged}
			refInfo={group.primary}
			repositoryId={repositoryId}
		>
			{pill}
		</BranchRefContextMenu>
	);
}
