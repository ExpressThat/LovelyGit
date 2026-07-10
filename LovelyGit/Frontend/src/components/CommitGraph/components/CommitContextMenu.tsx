import { ClipboardCopy, GitCommitHorizontal, Info } from "lucide-react";
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
	currentBranchName,
	isHead,
	onCherryPick,
	onOpenDetails,
	row,
}: {
	children: ReactNode;
	currentBranchName: string | null;
	isHead: boolean;
	onCherryPick: (row: CommitGraphRow) => void;
	onOpenDetails: (row: CommitGraphRow) => void;
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
					onClick={() => void copyToClipboard(row.commit.hash, "Commit hash")}
				>
					<ClipboardCopy aria-hidden="true" />
					Copy commit hash
				</ContextMenuItem>
				<ContextMenuSeparator />
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
			</ContextMenuContent>
		</ContextMenu>
	);
}
