import type { ReactNode } from "react";
import {
	Cloud,
	CloudUpload,
	Copy,
	GitBranch,
	GitCommitHorizontal,
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
import { copyToClipboard } from "../utils/clipboard";

export type TagAction = "checkout" | "delete" | "deleteRemote" | "push";

export function TagContextMenu({
	children,
	disabled,
	commitHash,
	inline = false,
	onAction,
	onCreateBranch,
	remoteName,
	tagName,
}: {
	children: ReactNode;
	disabled: boolean;
	commitHash: string;
	inline?: boolean;
	onAction: (action: TagAction, tagName: string) => void;
	onCreateBranch: (tagName: string, commitHash: string) => void;
	remoteName: string | null;
	tagName: string;
}) {
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
						{tagName}
					</ContextMenuLabel>
				</ContextMenuGroup>
				<ContextMenuSeparator />
				<ContextMenuItem
					onClick={() => void copyToClipboard(tagName, "Tag name")}
				>
					<Copy aria-hidden="true" />
					Copy tag name
				</ContextMenuItem>
				<ContextMenuItem
					className="text-destructive focus:bg-destructive/10 focus:text-destructive"
					disabled={disabled || remoteName === null}
					onClick={() => onAction("deleteRemote", tagName)}
					title={
						remoteName
							? `Delete ${tagName} from ${remoteName}`
							: "No remote is available"
					}
				>
					<Cloud aria-hidden="true" />
					Delete from {remoteName ?? "remote"}…
				</ContextMenuItem>
				<ContextMenuItem onClick={() => onCreateBranch(tagName, commitHash)}>
					<GitBranch aria-hidden="true" />
					Create branch from {tagName}…
				</ContextMenuItem>
				<ContextMenuItem
					disabled={disabled}
					onClick={() => onAction("checkout", tagName)}
				>
					<GitCommitHorizontal aria-hidden="true" />
					Checkout {tagName} detached…
				</ContextMenuItem>
				<ContextMenuItem
					disabled={disabled || remoteName === null}
					onClick={() => onAction("push", tagName)}
					title={
						remoteName
							? `Push ${tagName} to ${remoteName}`
							: "No remote is available"
					}
				>
					<CloudUpload aria-hidden="true" />
					<span className="min-w-0 truncate">
						Push to {remoteName ?? "remote"}
					</span>
				</ContextMenuItem>
				<ContextMenuSeparator />
				<ContextMenuItem
					className="text-destructive focus:bg-destructive/10 focus:text-destructive"
					disabled={disabled}
					onClick={() => onAction("delete", tagName)}
				>
					<Trash2 aria-hidden="true" />
					Delete local tag
				</ContextMenuItem>
			</ContextMenuContent>
		</ContextMenu>
	);
}
