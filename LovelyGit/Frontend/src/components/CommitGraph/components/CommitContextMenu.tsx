import { type ReactNode, useState } from "react";
import {
	ClipboardCopy,
	ExternalLink,
	GitBranch,
	GitCommitHorizontal,
	Info,
	ListRestart,
	ListTree,
	SearchCode,
	Tag,
	Undo2,
	Unplug,
} from "@/components/icons/lovelyIcons";
import { openRemoteWebResource } from "@/components/TopNavBar/components/RepositoryCommands";
import {
	ContextMenu,
	ContextMenuContent,
	ContextMenuGroup,
	ContextMenuItem,
	ContextMenuLabel,
	ContextMenuSeparator,
	ContextMenuTrigger,
} from "@/components/ui/context-menu";
import type { CommitGraphRow } from "@/generated/types";
import { copyToClipboard } from "../utils/clipboard";
import { shortHash } from "../utils/format";
import { CommitComparisonMenuItem } from "./CommitComparisonMenuItem";
import { CommitExportMenuItems } from "./CommitExportMenuItems";
import { StartBisectDialog } from "./StartBisectDialog";

export function CommitContextMenu({
	children,
	comparisonBase,
	copyPatchBusy,
	archiveBusy,
	savePatchBusy,
	currentBranchName,
	isHead,
	onCherryPick,
	onCheckoutCommit,
	onCompare,
	onCreateTag,
	onCreateBranch,
	onCopyPatch,
	onSaveArchive,
	onSavePatch,
	onOpenDetails,
	onSetComparisonBase,
	onInteractiveRebase,
	onRevert,
	onReset,
	repositoryId,
	row,
}: {
	children: ReactNode;
	comparisonBase: CommitGraphRow | null;
	copyPatchBusy: boolean;
	archiveBusy: boolean;
	savePatchBusy: boolean;
	currentBranchName: string | null;
	isHead: boolean;
	onCherryPick: (row: CommitGraphRow) => void;
	onCheckoutCommit: (row: CommitGraphRow) => void;
	onCompare: (row: CommitGraphRow) => void;
	onCreateTag: (row: CommitGraphRow) => void;
	onCreateBranch: (row: CommitGraphRow) => void;
	onCopyPatch: (row: CommitGraphRow) => void;
	onSaveArchive: (row: CommitGraphRow) => void;
	onSavePatch: (row: CommitGraphRow) => void;
	onOpenDetails: (row: CommitGraphRow) => void;
	onSetComparisonBase: (row: CommitGraphRow | null) => void;
	onInteractiveRebase: (row: CommitGraphRow) => void;
	onRevert: (row: CommitGraphRow) => void;
	onReset: (row: CommitGraphRow) => void;
	repositoryId: string | null;
	row: CommitGraphRow;
}) {
	const abbreviatedHash = shortHash(row.commit.hash);
	const subject = row.commit.message || "(no commit message)";
	const [bisectCommit, setBisectCommit] = useState<CommitGraphRow | null>(null);
	return (
		<>
			<ContextMenu>
				<ContextMenuTrigger className="block w-full">
					{children}
				</ContextMenuTrigger>
				<ContextMenuContent className="max-w-96">
					<ContextMenuGroup>
						<ContextMenuLabel className="grid max-w-88 gap-0.5 normal-case">
							<span className="truncate text-foreground">{subject}</span>
							<span className="font-mono font-normal text-[10px]">
								{abbreviatedHash}
							</span>
						</ContextMenuLabel>
					</ContextMenuGroup>
					<ContextMenuSeparator />
					<ContextMenuItem onClick={() => onOpenDetails(row)}>
						<Info aria-hidden="true" />
						Open commit details
					</ContextMenuItem>
					<ContextMenuItem
						disabled={currentBranchName === null && isHead}
						onClick={() => onCheckoutCommit(row)}
						title={
							currentBranchName === null && isHead
								? "This commit is already checked out in detached HEAD mode"
								: `Checkout ${abbreviatedHash} without moving a branch`
						}
					>
						<Unplug aria-hidden="true" />
						Checkout {abbreviatedHash} (detached)…
					</ContextMenuItem>
					<CommitComparisonMenuItem
						base={comparisonBase}
						onCompare={onCompare}
						onSetBase={onSetComparisonBase}
						row={row}
					/>
					<ContextMenuItem
						disabled={currentBranchName === null || isHead}
						onClick={() => onInteractiveRebase(row)}
						title={
							isHead
								? "Select an earlier commit as the rebase base"
								: `Edit commits after ${abbreviatedHash} on ${currentBranchName}`
						}
					>
						<ListTree aria-hidden="true" />
						<span className="min-w-0 truncate">
							Interactively rebase {currentBranchName ?? "current branch"} after{" "}
							{abbreviatedHash}…
						</span>
					</ContextMenuItem>
					<ContextMenuItem
						aria-label={`Start bisect with ${abbreviatedHash} as good`}
						disabled={currentBranchName === null || isHead}
						onClick={() => setBisectCommit(row)}
						title={
							currentBranchName === null
								? "Check out a branch before starting bisect"
								: isHead
									? "Choose an earlier known-good commit"
									: `Mark ${abbreviatedHash} good and HEAD bad`
						}
					>
						<SearchCode aria-hidden="true" />
						<span className="min-w-0 truncate">
							Start bisect: {abbreviatedHash} good, HEAD bad…
						</span>
					</ContextMenuItem>
					<ContextMenuItem
						onClick={() => void copyToClipboard(row.commit.hash, "Commit hash")}
					>
						<ClipboardCopy aria-hidden="true" />
						Copy commit hash
					</ContextMenuItem>
					<ContextMenuItem
						disabled={!repositoryId}
						onClick={() =>
							repositoryId &&
							void openRemoteWebResource(
								repositoryId,
								"Commit",
								row.commit.hash,
							)
						}
					>
						<ExternalLink aria-hidden="true" />
						Open commit on remote website
					</ContextMenuItem>
					<CommitExportMenuItems
						archiveBusy={archiveBusy}
						copyPatchBusy={copyPatchBusy}
						onCopyPatch={onCopyPatch}
						onSaveArchive={onSaveArchive}
						onSavePatch={onSavePatch}
						row={row}
						savePatchBusy={savePatchBusy}
					/>
					<ContextMenuSeparator />
					<ContextMenuItem onClick={() => onCreateTag(row)}>
						<Tag aria-hidden="true" />
						Create tag at {abbreviatedHash}
					</ContextMenuItem>
					<ContextMenuItem onClick={() => onCreateBranch(row)}>
						<GitBranch aria-hidden="true" />
						Create branch at {abbreviatedHash}…
					</ContextMenuItem>
					<ContextMenuItem
						disabled={currentBranchName === null}
						onClick={() => onRevert(row)}
						title={
							currentBranchName === null
								? "Check out a branch before reverting"
								: `Revert ${abbreviatedHash} on ${currentBranchName}`
						}
					>
						<Undo2 aria-hidden="true" />
						<span className="min-w-0 truncate">
							Revert {abbreviatedHash} on {currentBranchName ?? "a branch"}
						</span>
					</ContextMenuItem>
					<ContextMenuItem
						disabled={currentBranchName === null || isHead}
						onClick={() => onCherryPick(row)}
						title={
							isHead
								? "This commit is already checked out"
								: currentBranchName === null
									? "Check out a branch before cherry-picking"
									: `Cherry-pick ${abbreviatedHash} onto ${currentBranchName}`
						}
					>
						<GitCommitHorizontal aria-hidden="true" />
						<span className="min-w-0 truncate">
							Cherry-pick {abbreviatedHash} onto{" "}
							{currentBranchName ?? "a branch"}
						</span>
					</ContextMenuItem>
					<ContextMenuItem
						disabled={currentBranchName === null || isHead}
						onClick={() => onReset(row)}
						title={
							currentBranchName
								? `Reset ${currentBranchName} to ${abbreviatedHash}`
								: "Check out a branch before resetting"
						}
					>
						<ListRestart aria-hidden="true" />
						<span className="min-w-0 truncate">
							Reset {currentBranchName ?? "current branch"} to {abbreviatedHash}
							…
						</span>
					</ContextMenuItem>
				</ContextMenuContent>
			</ContextMenu>
			{bisectCommit ? (
				<StartBisectDialog
					commit={bisectCommit}
					onOpenChange={(open) => !open && setBisectCommit(null)}
					repositoryId={repositoryId}
				/>
			) : null}
		</>
	);
}
