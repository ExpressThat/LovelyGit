import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import type { CommitGraphRow } from "@/generated/types";
import { type BranchAction, BranchContextMenu } from "./BranchContextMenu";
import {
	buildLegacyRefs,
	groupRefs,
	normalizeRefs,
	type RefGroup,
	RefIcon,
	refLabelForRemotes,
	uniqueKinds,
} from "./RefCellUtils";
import { type TagAction, TagContextMenu } from "./TagContextMenu";

export function RefCell({
	branchMutationBusy,
	branchRemoteName,
	currentBranchName,
	onBranchAction,
	onCreateBranchFromTag,
	onIntegrateBranch,
	onTagAction,
	remotePrefixes,
	row,
	tagMutationBusy,
	tagRemoteName,
}: {
	branchMutationBusy: boolean;
	branchRemoteName: string | null;
	currentBranchName: string | null;
	onBranchAction: (action: BranchAction, branchName: string) => void;
	onCreateBranchFromTag: (tagName: string, commitHash: string) => void;
	onIntegrateBranch: (mode: BranchIntegrationMode, branchName: string) => void;
	onTagAction: (action: TagAction, tagName: string) => void;
	remotePrefixes: string[];
	row: CommitGraphRow;
	tagMutationBusy: boolean;
	tagRemoteName: string | null;
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
					branchMutationBusy={branchMutationBusy}
					branchRemoteName={branchRemoteName}
					currentBranchName={currentBranchName}
					commitHash={row.commit.hash}
					group={group}
					key={group.key}
					onBranchAction={onBranchAction}
					onCreateBranchFromTag={onCreateBranchFromTag}
					onIntegrateBranch={onIntegrateBranch}
					onTagAction={onTagAction}
					remotePrefixes={remotePrefixes}
					tagMutationBusy={tagMutationBusy}
					tagRemoteName={tagRemoteName}
				/>
			))}
		</div>
	);
}
function RefGroupPill({
	branchMutationBusy,
	branchRemoteName,
	currentBranchName,
	commitHash,
	group,
	onBranchAction,
	onCreateBranchFromTag,
	onIntegrateBranch,
	onTagAction,
	remotePrefixes,
	tagMutationBusy,
	tagRemoteName,
}: {
	branchMutationBusy: boolean;
	branchRemoteName: string | null;
	currentBranchName: string | null;
	commitHash: string;
	group: RefGroup;
	onIntegrateBranch: (mode: BranchIntegrationMode, branchName: string) => void;
	onBranchAction: (action: BranchAction, branchName: string) => void;
	onCreateBranchFromTag: (tagName: string, commitHash: string) => void;
	onTagAction: (action: TagAction, tagName: string) => void;
	remotePrefixes: string[];
	tagMutationBusy: boolean;
	tagRemoteName: string | null;
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
	const tag = group.refs.find((ref) => ref.kind === "Tag");
	return localBranch ? (
		<BranchContextMenu
			branchName={localBranch.name}
			currentBranchName={currentBranchName}
			disabled={branchMutationBusy}
			inline
			onAction={onBranchAction}
			onIntegrateBranch={onIntegrateBranch}
			remoteName={branchRemoteName}
		>
			{pill}
		</BranchContextMenu>
	) : tag ? (
		<TagContextMenu
			commitHash={commitHash}
			disabled={tagMutationBusy}
			inline
			onAction={onTagAction}
			onCreateBranch={onCreateBranchFromTag}
			remoteName={tagRemoteName}
			tagName={tag.name}
		>
			{pill}
		</TagContextMenu>
	) : (
		pill
	);
}
