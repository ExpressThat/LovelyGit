import {
	CloudDownload,
	GitCompareArrows,
	GitMerge,
	ListRestart,
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
import type { BranchAction } from "./BranchContextMenu";

export function RemoteBranchContextMenu({
	children,
	currentBranchName,
	disabled,
	inline = false,
	onAction,
	onIntegrateBranch,
	remoteBranchName,
}: {
	children: ReactNode;
	currentBranchName: string | null;
	disabled: boolean;
	inline?: boolean;
	onAction: (action: BranchAction, branchName: string) => void;
	onIntegrateBranch: (mode: BranchIntegrationMode, branchName: string) => void;
	remoteBranchName: string;
}) {
	const canIntegrate = !disabled && currentBranchName !== null;
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
						{remoteBranchName}
					</ContextMenuLabel>
				</ContextMenuGroup>
				<ContextMenuSeparator />
				<ContextMenuItem
					disabled={!canIntegrate}
					onClick={() => onAction("compare", remoteBranchName)}
				>
					<GitCompareArrows aria-hidden="true" />
					<span className="min-w-0 truncate">
						Compare {currentBranchName ?? "current branch"} with{" "}
						{remoteBranchName}…
					</span>
				</ContextMenuItem>
				<ContextMenuItem
					disabled={disabled}
					onClick={() => onAction("checkoutRemote", remoteBranchName)}
				>
					<CloudDownload aria-hidden="true" />
					Check out as local branch…
				</ContextMenuItem>
				<ContextMenuSeparator />
				<ContextMenuItem
					disabled={!canIntegrate}
					onClick={() => onIntegrateBranch("merge", remoteBranchName)}
				>
					<GitMerge aria-hidden="true" />
					<span className="min-w-0 truncate">
						Merge {remoteBranchName} into{" "}
						{currentBranchName ?? "current branch"}
					</span>
				</ContextMenuItem>
				<ContextMenuItem
					disabled={!canIntegrate}
					onClick={() => onIntegrateBranch("rebase", remoteBranchName)}
				>
					<ListRestart aria-hidden="true" />
					<span className="min-w-0 truncate">
						Rebase {currentBranchName ?? "current branch"} onto{" "}
						{remoteBranchName}
					</span>
				</ContextMenuItem>
				<ContextMenuSeparator />
				<ContextMenuItem
					className="text-destructive focus:bg-destructive/10 focus:text-destructive"
					disabled={disabled}
					onClick={() => onAction("deleteRemote", remoteBranchName)}
				>
					<Trash2 aria-hidden="true" />
					Delete from remote…
				</ContextMenuItem>
			</ContextMenuContent>
		</ContextMenu>
	);
}
