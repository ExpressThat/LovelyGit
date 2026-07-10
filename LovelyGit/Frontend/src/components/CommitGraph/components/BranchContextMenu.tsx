import { GitMerge, ListRestart } from "lucide-react";
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
	inline = false,
	onIntegrateBranch,
}: {
	branchName: string;
	children: ReactNode;
	currentBranchName: string | null;
	inline?: boolean;
	onIntegrateBranch: (mode: BranchIntegrationMode, branchName: string) => void;
}) {
	const canIntegrate =
		currentBranchName !== null && branchName !== currentBranchName;
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
			</ContextMenuContent>
		</ContextMenu>
	);
}
