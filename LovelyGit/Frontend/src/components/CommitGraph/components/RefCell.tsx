import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import type { CommitGraphRow } from "@/generated/types";
import { BranchContextMenu } from "./BranchContextMenu";
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
	onIntegrateBranch,
	remotePrefixes,
	row,
}: {
	currentBranchName: string | null;
	onIntegrateBranch: (mode: BranchIntegrationMode, branchName: string) => void;
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

	return (
		<div className="flex min-w-0 gap-1 overflow-hidden">
			{groups.map((group) => (
				<RefGroupPill
					currentBranchName={currentBranchName}
					group={group}
					key={group.key}
					onIntegrateBranch={onIntegrateBranch}
					remotePrefixes={remotePrefixes}
				/>
			))}
		</div>
	);
}
function RefGroupPill({
	currentBranchName,
	group,
	onIntegrateBranch,
	remotePrefixes,
}: {
	currentBranchName: string | null;
	group: RefGroup;
	onIntegrateBranch: (mode: BranchIntegrationMode, branchName: string) => void;
	remotePrefixes: string[];
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
	const localBranch = group.refs.find((ref) => ref.kind === "Local");
	return localBranch ? (
		<BranchContextMenu
			branchName={localBranch.name}
			currentBranchName={currentBranchName}
			inline
			onIntegrateBranch={onIntegrateBranch}
		>
			{pill}
		</BranchContextMenu>
	) : (
		pill
	);
}
