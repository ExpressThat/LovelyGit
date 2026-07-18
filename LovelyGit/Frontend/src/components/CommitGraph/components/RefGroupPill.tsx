import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import { type BranchAction, BranchContextMenu } from "./BranchContextMenu";
import {
	type RefGroup,
	RefIcon,
	refLabelForRemotes,
	uniqueKinds,
} from "./RefCellUtils";
import { RemoteBranchContextMenu } from "./RemoteBranchContextMenu";
import { type TagAction, TagContextMenu } from "./TagContextMenu";

export function RefGroupPill({
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
	wide = false,
}: RefGroupPillProps) {
	const icons = uniqueKinds(group.refs);
	const pill = (
		<span
			className={`inline-flex h-[14px] shrink-0 cursor-pointer items-center gap-1 overflow-hidden whitespace-nowrap rounded-[3px] border border-border bg-secondary px-1 text-[10px] text-secondary-foreground shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring ${wide ? "w-full max-w-full" : "max-w-[132px]"}`}
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
	const remoteBranch = group.refs.find((ref) => ref.kind === "Remote");
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
	) : remoteBranch ? (
		<RemoteBranchContextMenu
			currentBranchName={currentBranchName}
			disabled={branchMutationBusy}
			inline
			onAction={onBranchAction}
			onIntegrateBranch={onIntegrateBranch}
			remoteBranchName={remoteBranch.name}
		>
			{pill}
		</RemoteBranchContextMenu>
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

export type RefGroupPillProps = {
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
	wide?: boolean;
};
