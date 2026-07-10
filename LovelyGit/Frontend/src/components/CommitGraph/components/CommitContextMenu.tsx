import {
	ClipboardCopy,
	Copy,
	GitBranch,
	GitCommitHorizontal,
	Info,
	ListRestart,
	ListTree,
	Tag,
	Undo2,
} from "lucide-react";
import type { ReactNode } from "react";
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

export function CommitContextMenu({
	children,
	copyPatchBusy,
	currentBranchName,
	isHead,
	onCherryPick,
	onCreateTag,
	onCreateBranch,
	onCopyPatch,
	onOpenDetails,
	onInteractiveRebase,
	onRevert,
	onReset,
	row,
}: {
	children: ReactNode;
	copyPatchBusy: boolean;
	currentBranchName: string | null;
	isHead: boolean;
	onCherryPick: (row: CommitGraphRow) => void;
	onCreateTag: (row: CommitGraphRow) => void;
	onCreateBranch: (row: CommitGraphRow) => void;
	onCopyPatch: (row: CommitGraphRow) => void;
	onOpenDetails: (row: CommitGraphRow) => void;
	onInteractiveRebase: (row: CommitGraphRow) => void;
	onRevert: (row: CommitGraphRow) => void;
	onReset: (row: CommitGraphRow) => void;
	row: CommitGraphRow;
}) {
	const abbreviatedHash = shortHash(row.commit.hash);
	const subject =
		row.commit.message.split(/\r?\n/, 1)[0] || "(no commit message)";
	return (
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
					onClick={() => void copyToClipboard(row.commit.hash, "Commit hash")}
				>
					<ClipboardCopy aria-hidden="true" />
					Copy commit hash
				</ContextMenuItem>
				<ContextMenuItem
					disabled={copyPatchBusy}
					onClick={() => onCopyPatch(row)}
				>
					<Copy aria-hidden="true" />
					{copyPatchBusy ? "Creating commit patch…" : "Copy commit as patch"}
				</ContextMenuItem>
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
						Cherry-pick {abbreviatedHash} onto {currentBranchName ?? "a branch"}
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
						Reset {currentBranchName ?? "current branch"} to {abbreviatedHash}…
					</span>
				</ContextMenuItem>
			</ContextMenuContent>
		</ContextMenu>
	);
}
