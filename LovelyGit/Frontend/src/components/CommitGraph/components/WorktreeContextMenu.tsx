import type { ReactNode } from "react";
import {
	FolderGit2,
	FolderOpen,
	LockKeyhole,
	LockKeyholeOpen,
	SquareTerminal,
	Trash2,
} from "@/components/icons/lovelyIcons";
import {
	ContextMenu,
	ContextMenuContent,
	ContextMenuGroup,
	ContextMenuItem,
	ContextMenuLabel,
	ContextMenuSeparator,
	ContextMenuTrigger,
} from "@/components/ui/context-menu";
import type {
	RepositoryWorktreeItem,
	WorktreeMutationAction,
} from "@/generated/types";

export function WorktreeContextMenu({
	children,
	disabled,
	onAction,
	worktree,
}: {
	children: ReactNode;
	disabled: boolean;
	onAction: (
		action: WorktreeMutationAction,
		worktree: RepositoryWorktreeItem,
	) => void;
	worktree: RepositoryWorktreeItem;
}) {
	const label = worktree.branchName ?? "Detached HEAD";
	return (
		<ContextMenu>
			<ContextMenuTrigger onContextMenu={(event) => event.stopPropagation()}>
				{children}
			</ContextMenuTrigger>
			<ContextMenuContent>
				<ContextMenuGroup>
					<ContextMenuLabel className="max-w-72 truncate font-mono">
						{label}
					</ContextMenuLabel>
				</ContextMenuGroup>
				<ContextMenuSeparator />
				<ContextMenuItem
					disabled={disabled || worktree.isCurrent}
					onClick={() => onAction("Open", worktree)}
				>
					<FolderGit2 aria-hidden="true" />
					Open in LovelyGit
				</ContextMenuItem>
				<ContextMenuItem
					disabled={disabled}
					onClick={() => onAction("Reveal", worktree)}
				>
					<FolderOpen aria-hidden="true" />
					Reveal in file manager
				</ContextMenuItem>
				<ContextMenuItem
					disabled={disabled}
					onClick={() => onAction("Terminal", worktree)}
				>
					<SquareTerminal aria-hidden="true" />
					Open terminal here
				</ContextMenuItem>
				<ContextMenuSeparator />
				{worktree.isLocked ? (
					<ContextMenuItem
						disabled={disabled || worktree.isCurrent}
						onClick={() => onAction("Unlock", worktree)}
					>
						<LockKeyholeOpen aria-hidden="true" />
						Unlock worktree
					</ContextMenuItem>
				) : (
					<ContextMenuItem
						disabled={disabled || worktree.isCurrent}
						onClick={() => onAction("Lock", worktree)}
					>
						<LockKeyhole aria-hidden="true" />
						Lock worktree…
					</ContextMenuItem>
				)}
				<ContextMenuItem
					className="text-destructive focus:bg-destructive/10 focus:text-destructive"
					disabled={disabled || worktree.isCurrent}
					onClick={() => onAction("Remove", worktree)}
				>
					<Trash2 aria-hidden="true" />
					Remove worktree…
				</ContextMenuItem>
			</ContextMenuContent>
		</ContextMenu>
	);
}
