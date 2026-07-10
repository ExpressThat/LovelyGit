import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import { Button } from "@/components/ui/button";
import type { CommitGraphRow } from "@/generated/types";
import { shortHash } from "../utils/format";
import { type BranchAction, BranchContextMenu } from "./BranchContextMenu";
import { RefIcon } from "./RefCellUtils";
import type { RefPanelItem, RefPanelSection } from "./RefsPanelData";
import { type TagAction, TagContextMenu } from "./TagContextMenu";

export function RefSection({
	branchMutationBusy,
	branchRemoteName,
	currentBranchName,
	onBranchAction,
	onIntegrateBranch,
	onSelectCommit,
	onTagAction,
	section,
	tagMutationBusy,
	tagRemoteName,
}: {
	branchMutationBusy: boolean;
	branchRemoteName: string | null;
	currentBranchName: string | null;
	onIntegrateBranch: (mode: BranchIntegrationMode, branchName: string) => void;
	onBranchAction: (action: BranchAction, branchName: string) => void;
	onSelectCommit: (row: CommitGraphRow) => void;
	onTagAction: (action: TagAction, tagName: string) => void;
	section: RefPanelSection;
	tagMutationBusy: boolean;
	tagRemoteName: string | null;
}) {
	return (
		<section className="mb-3 last:mb-0">
			<div className="mb-1 flex items-center justify-between px-1 text-[10px] font-semibold uppercase text-muted-foreground">
				<span>{section.label}</span>
				<span>{section.count}</span>
			</div>
			<div className="grid gap-1">
				{section.items.map((item) => (
					<RefPanelRow
						branchMutationBusy={branchMutationBusy}
						branchRemoteName={branchRemoteName}
						currentBranchName={currentBranchName}
						item={item}
						key={`${item.kind}:${item.name}:${item.commitHash}`}
						onIntegrateBranch={onIntegrateBranch}
						onBranchAction={onBranchAction}
						onSelectCommit={onSelectCommit}
						onTagAction={onTagAction}
						tagMutationBusy={tagMutationBusy}
						tagRemoteName={tagRemoteName}
					/>
				))}
			</div>
		</section>
	);
}

export function RefPanelRow({
	branchMutationBusy,
	branchRemoteName,
	currentBranchName,
	item,
	onBranchAction,
	onIntegrateBranch,
	onSelectCommit,
	onTagAction,
	tagMutationBusy,
	tagRemoteName,
}: {
	branchMutationBusy: boolean;
	branchRemoteName: string | null;
	currentBranchName: string | null;
	item: RefPanelItem;
	onIntegrateBranch: (mode: BranchIntegrationMode, branchName: string) => void;
	onBranchAction: (action: BranchAction, branchName: string) => void;
	onSelectCommit: (row: CommitGraphRow) => void;
	onTagAction: (action: TagAction, tagName: string) => void;
	tagMutationBusy: boolean;
	tagRemoteName: string | null;
}) {
	const row = item.row;
	const branchButton = (
		<Button
			aria-disabled={!row}
			className="h-7 min-w-0 justify-start gap-2 px-2 font-normal"
			onClick={() => {
				if (row) onSelectCommit(row);
			}}
			title={
				row
					? `${item.name} at ${shortHash(item.commitHash)}`
					: `${item.name} at ${shortHash(item.commitHash)}. Load this commit in the graph to select it.`
			}
			variant={item.isCurrent ? "secondary" : "ghost"}
		>
			<RefIcon kind={item.kind} />
			<span className="min-w-0 flex-1 truncate text-left">{item.label}</span>
			<span className="font-mono text-[10px] text-muted-foreground">
				{shortHash(item.commitHash)}
			</span>
		</Button>
	);

	if (item.kind !== "Local") {
		return item.kind === "Tag" ? (
			<TagContextMenu
				disabled={tagMutationBusy}
				onAction={onTagAction}
				remoteName={tagRemoteName}
				tagName={item.name}
			>
				{branchButton}
			</TagContextMenu>
		) : (
			branchButton
		);
	}

	return (
		<BranchContextMenu
			branchName={item.name}
			currentBranchName={currentBranchName}
			disabled={branchMutationBusy}
			onAction={onBranchAction}
			onIntegrateBranch={onIntegrateBranch}
			remoteName={branchRemoteName}
		>
			{branchButton}
		</BranchContextMenu>
	);
}
