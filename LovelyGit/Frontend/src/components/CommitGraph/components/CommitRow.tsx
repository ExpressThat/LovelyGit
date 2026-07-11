/** biome-ignore-all lint/suspicious/noArrayIndexKey: shimmer has no id */
import type { ReactNode } from "react";
import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import type { CommitGraphRow } from "@/generated/types";
import type { CommitComparisonController } from "../hooks/useCommitGraphDialogs";
import { AuthorCell } from "./AuthorCell";
import type { BranchAction } from "./BranchContextMenu";
import { CommitContextMenu } from "./CommitContextMenu";
import { CommitMessage } from "./CommitMessage";
import { GraphCell } from "./GraphCell";
import { HashCell } from "./HashCell";
import { RefCell } from "./RefCell";
import { SkeletonShimmer } from "./SkeletonShimmer";
import type { TagAction } from "./TagContextMenu";

export function CommitRow({
	branchMutationBusy,
	branchRemoteName,
	copyPatchBusy,
	comparison,
	savePatchBusy,
	currentBranchName,
	graph,
	isSelected,
	isHead,
	onCherryPick,
	onBranchAction,
	onCreateTag,
	onCreateBranch,
	onCopyPatch,
	onSavePatch,
	onCreateBranchFromTag,
	onIntegrateBranch,
	onInteractiveRebase,
	onRevert,
	onReset,
	onSelect,
	onTagAction,
	remotePrefixes,
	row,
	rowIndex,
	tagMutationBusy,
	tagRemoteName,
	templateColumns,
}: {
	branchMutationBusy: boolean;
	branchRemoteName: string | null;
	copyPatchBusy: boolean;
	comparison: CommitComparisonController;
	savePatchBusy: boolean;
	currentBranchName: string | null;
	graph: {
		contentWidth: number;
		scrollLeft: number;
	};
	isSelected: boolean;
	isHead: boolean;
	onIntegrateBranch: (mode: BranchIntegrationMode, branchName: string) => void;
	onInteractiveRebase: (row: CommitGraphRow) => void;
	onRevert: (row: CommitGraphRow) => void;
	onReset: (row: CommitGraphRow) => void;
	onCherryPick: (row: CommitGraphRow) => void;
	onBranchAction: (action: BranchAction, branchName: string) => void;
	onCreateTag: (row: CommitGraphRow) => void;
	onCreateBranch: (row: CommitGraphRow) => void;
	onCopyPatch: (row: CommitGraphRow) => void;
	onSavePatch: (row: CommitGraphRow) => void;
	onCreateBranchFromTag: (tagName: string, commitHash: string) => void;
	onSelect: (row: CommitGraphRow) => void;
	onTagAction: (action: TagAction, tagName: string) => void;
	remotePrefixes: string[];
	row: CommitGraphRow | null;
	rowIndex: number;
	tagMutationBusy: boolean;
	tagRemoteName: string | null;
	templateColumns: string;
}) {
	const rowClassName = `grid h-[22px] leading-[22px] ${isSelected ? "bg-primary/12 ring-1 ring-inset ring-primary/35 dark:bg-primary/20" : rowIndex % 2 === 0 ? "bg-background dark:bg-background" : "bg-card/70 dark:bg-card/45"} hover:bg-accent/75 dark:hover:bg-accent/60`;

	if (!row) {
		return (
			<div
				className={`relative overflow-hidden ${rowClassName}`}
				style={{ gridTemplateColumns: templateColumns }}
			>
				<Column className="px-[6px] py-[2px]">
					<SkeletonShimmer className="mt-[7px] block h-2 w-20 rounded-full" />
				</Column>
				<Column className="bg-card/60 px-2">
					<div className="flex h-full items-center gap-2">
						<SkeletonShimmer className="block h-1 w-8 rounded-full" />
						<SkeletonShimmer className="block h-1 w-10 rounded-full" />
						<SkeletonShimmer className="block h-[6px] w-[6px] rounded-full" />
					</div>
				</Column>
				<Column className="px-2">
					<SkeletonShimmer className="mt-[7px] block h-2 w-[240px] max-w-full rounded-full" />
				</Column>
				<Column className="px-2">
					<SkeletonShimmer className="mt-[7px] block h-2 w-14 rounded-full" />
				</Column>
				<Column className="px-2" isLast>
					<SkeletonShimmer className="mt-[7px] block h-2 w-28 rounded-full" />
				</Column>
			</div>
		);
	}

	const commitButton = (
		<button
			className={`${rowClassName} w-full cursor-pointer border-0 p-0 text-left text-inherit focus-visible:bg-accent focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-inset focus-visible:ring-ring`}
			onClick={() => onSelect(row)}
			style={{ gridTemplateColumns: templateColumns }}
			type="button"
		>
			<Column className="px-[6px] py-[2px]">
				<RefCell
					branchMutationBusy={branchMutationBusy}
					branchRemoteName={branchRemoteName}
					currentBranchName={currentBranchName}
					onBranchAction={onBranchAction}
					onIntegrateBranch={onIntegrateBranch}
					onTagAction={onTagAction}
					onCreateBranchFromTag={onCreateBranchFromTag}
					remotePrefixes={remotePrefixes}
					row={row}
					tagMutationBusy={tagMutationBusy}
					tagRemoteName={tagRemoteName}
				/>
			</Column>
			<Column className="bg-card/60">
				<GraphCell
					graphContentWidth={graph.contentWidth}
					graphScrollLeft={graph.scrollLeft}
					row={row}
				/>
			</Column>
			<Column className="px-2">
				<CommitMessage row={row} />
			</Column>
			<Column className="px-2">
				<HashCell row={row} />
			</Column>
			<Column className="px-2" isLast>
				<AuthorCell row={row} />
			</Column>
		</button>
	);

	return (
		<CommitContextMenu
			comparisonBase={comparison.base}
			copyPatchBusy={copyPatchBusy}
			savePatchBusy={savePatchBusy}
			currentBranchName={currentBranchName}
			isHead={isHead}
			onCherryPick={onCherryPick}
			onCompare={comparison.compare}
			onCreateTag={onCreateTag}
			onCreateBranch={onCreateBranch}
			onCopyPatch={onCopyPatch}
			onSavePatch={onSavePatch}
			onOpenDetails={onSelect}
			onSetComparisonBase={comparison.setBase}
			onInteractiveRebase={onInteractiveRebase}
			onRevert={onRevert}
			onReset={onReset}
			row={row}
		>
			{commitButton}
		</CommitContextMenu>
	);
}

function Column({
	children,
	className,
	isLast = false,
}: {
	children: ReactNode;
	className: string;
	isLast?: boolean;
}) {
	return (
		<div
			className={`min-w-0 overflow-hidden ${isLast ? "" : "border-r"} ${className}`}
		>
			{children}
		</div>
	);
}
