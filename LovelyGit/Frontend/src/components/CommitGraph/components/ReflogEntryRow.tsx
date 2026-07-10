import { Clipboard, GitBranch, ListRestart } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
	ContextMenu,
	ContextMenuContent,
	ContextMenuGroup,
	ContextMenuItem,
	ContextMenuLabel,
	ContextMenuSeparator,
	ContextMenuTrigger,
} from "@/components/ui/context-menu";
import type { GitReflogEntry } from "@/generated/types";
import { shortHash } from "../utils/format";

export function ReflogEntryRow({
	entry,
	onCopy,
	onCreateBranch,
	onReset,
}: {
	entry: GitReflogEntry;
	onCopy: (hash: string) => void;
	onCreateBranch: (entry: GitReflogEntry) => void;
	onReset: (entry: GitReflogEntry) => void;
}) {
	const label = entry.message || "Reference moved";
	const row = (
		<div className="group grid min-w-0 grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-3 rounded-lg border bg-card px-3 py-2 transition-colors hover:bg-accent">
			<span className="rounded-md bg-muted px-2 py-1 font-mono text-[10px] text-muted-foreground">
				{entry.selector}
			</span>
			<span className="grid min-w-0 gap-0.5">
				<span className="truncate font-medium text-sm" title={label}>
					{label}
				</span>
				<span className="truncate text-muted-foreground text-xs">
					{entry.actorName || entry.actorEmail || "Unknown actor"} ·{" "}
					{formatDate(entry.timestampUnixSeconds)}
				</span>
			</span>
			<span className="flex items-center gap-1">
				<span className="mr-1 font-mono text-muted-foreground text-xs">
					{shortHash(entry.newHash)}
				</span>
				<Button
					aria-label={`Create recovery branch at ${entry.selector}`}
					className="opacity-60 group-hover:opacity-100 group-focus-within:opacity-100"
					onClick={() => onCreateBranch(entry)}
					size="icon-xs"
					title="Create recovery branch"
					variant="ghost"
				>
					<GitBranch aria-hidden="true" />
				</Button>
				<Button
					aria-label={`Reset current branch to ${entry.selector}`}
					className="opacity-60 group-hover:opacity-100 group-focus-within:opacity-100"
					onClick={() => onReset(entry)}
					size="icon-xs"
					title="Reset current branch here"
					variant="ghost"
				>
					<ListRestart aria-hidden="true" />
				</Button>
			</span>
		</div>
	);
	return (
		<ContextMenu>
			<ContextMenuTrigger>{row}</ContextMenuTrigger>
			<ContextMenuContent>
				<ContextMenuGroup>
					<ContextMenuLabel className="font-mono">
						{entry.selector}
					</ContextMenuLabel>
				</ContextMenuGroup>
				<ContextMenuSeparator />
				<ContextMenuItem onClick={() => onCreateBranch(entry)}>
					<GitBranch aria-hidden="true" /> Create recovery branch…
				</ContextMenuItem>
				<ContextMenuItem onClick={() => onReset(entry)}>
					<ListRestart aria-hidden="true" /> Reset current branch here…
				</ContextMenuItem>
				<ContextMenuItem onClick={() => onCopy(entry.newHash)}>
					<Clipboard aria-hidden="true" /> Copy commit hash
				</ContextMenuItem>
			</ContextMenuContent>
		</ContextMenu>
	);
}

function formatDate(timestamp: number) {
	return new Date(timestamp * 1000).toLocaleString();
}
