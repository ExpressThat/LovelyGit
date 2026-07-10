import {
	CloudUpload,
	FolderGit2,
	GitBranch,
	GitCompareArrows,
	GitMerge,
	History,
	Link2,
	ListRestart,
	Pencil,
	Trash2,
} from "lucide-react";
import type { ReactNode } from "react";
import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import {
	ContextMenu,
	ContextMenuContent,
	ContextMenuGroup,
	ContextMenuItem,
	ContextMenuLabel,
	ContextMenuSeparator,
	ContextMenuTrigger,
} from "@/components/ui/context-menu";

export function BranchContextMenu({
	branchName,
	children,
	currentBranchName,
	disabled,
	inline = false,
	onAction,
	onIntegrateBranch,
	remoteName,
}: {
	branchName: string;
	children: ReactNode;
	currentBranchName: string | null;
	disabled: boolean;
	inline?: boolean;
	onAction: (action: BranchAction, branchName: string) => void;
	onIntegrateBranch: (mode: BranchIntegrationMode, branchName: string) => void;
	remoteName: string | null;
}) {
	const canIntegrate =
		!disabled && currentBranchName !== null && branchName !== currentBranchName;
	const isCurrent = branchName === currentBranchName;
	return (
		<ContextMenu>
			<ContextMenuTrigger
				className={inline ? "inline-flex min-w-0" : "w-full"}
				onContextMenu={(event) => event.stopPropagation()}
				render={inline ? <span /> : undefined}
			>
				{children}
			</ContextMenuTrigger>
			<ContextMenuContent>
				<ContextMenuGroup>
					<ContextMenuLabel className="max-w-72 truncate font-mono">
						{branchName}
					</ContextMenuLabel>
				</ContextMenuGroup>
				<ContextMenuSeparator />
				<ContextMenuItem
					disabled={!canIntegrate}
					onClick={() => onAction("compare", branchName)}
				>
					<GitCompareArrows aria-hidden="true" />
					<span className="min-w-0 truncate">
						Compare {currentBranchName ?? "current branch"} with {branchName}…
					</span>
				</ContextMenuItem>
				<ContextMenuItem
					disabled={disabled || isCurrent}
					onClick={() => onAction("checkout", branchName)}
				>
					<GitBranch aria-hidden="true" />
					Switch to {branchName}
				</ContextMenuItem>
				<ContextMenuItem
					disabled={disabled || remoteName === null}
					onClick={() => onAction("push", branchName)}
				>
					<CloudUpload aria-hidden="true" />
					Push to {remoteName ?? "remote"}
				</ContextMenuItem>
				<ContextMenuItem
					disabled={disabled}
					onClick={() => onAction("upstream", branchName)}
				>
					<Link2 aria-hidden="true" />
					Manage upstream…
				</ContextMenuItem>
				<ContextMenuItem
					disabled={disabled}
					onClick={() => onAction("reflog", branchName)}
				>
					<History aria-hidden="true" />
					View reflog…
				</ContextMenuItem>
				<ContextMenuItem
					disabled={disabled}
					onClick={() => onAction("worktree", branchName)}
				>
					<FolderGit2 aria-hidden="true" />
					Create linked worktree…
				</ContextMenuItem>
				<ContextMenuSeparator />
				<ContextMenuItem
					disabled={!canIntegrate}
					onClick={() => onIntegrateBranch("merge", branchName)}
				>
					<GitMerge aria-hidden="true" />
					<span className="min-w-0 truncate">
						Merge {branchName} into {currentBranchName ?? "current branch"}
					</span>
				</ContextMenuItem>
				<ContextMenuItem
					disabled={!canIntegrate}
					onClick={() => onIntegrateBranch("rebase", branchName)}
				>
					<ListRestart aria-hidden="true" />
					<span className="min-w-0 truncate">
						Rebase {currentBranchName ?? "current branch"} onto {branchName}
					</span>
				</ContextMenuItem>
				<ContextMenuSeparator />
				<ContextMenuItem
					disabled={disabled}
					onClick={() => onAction("rename", branchName)}
				>
					<Pencil aria-hidden="true" />
					Rename branch…
				</ContextMenuItem>
				<ContextMenuItem
					className="text-destructive focus:bg-destructive/10 focus:text-destructive"
					disabled={disabled || isCurrent}
					onClick={() => onAction("delete", branchName)}
				>
					<Trash2 aria-hidden="true" />
					Delete local branch…
				</ContextMenuItem>
			</ContextMenuContent>
		</ContextMenu>
	);
}

export type BranchAction =
	| "checkout"
	| "compare"
	| "delete"
	| "push"
	| "reflog"
	| "rename"
	| "upstream"
	| "worktree";
