import { useState } from "react";
import {
	FolderGit2,
	FolderOpen,
	LoaderCircle,
	Trash2,
} from "@/components/icons/lovelyIcons";
import { revealKnownRepository } from "@/components/TopNavBar/components/RepositoryCommands";
import {
	AlertDialog,
	AlertDialogAction,
	AlertDialogCancel,
	AlertDialogContent,
	AlertDialogDescription,
	AlertDialogFooter,
	AlertDialogHeader,
	AlertDialogMedia,
	AlertDialogTitle,
} from "@/components/ui/alert-dialog";
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
import type { KnownGitRepository } from "@/generated/types";

export function RecentRepositoryRow({
	onOpen,
	onRemove,
	repository,
}: {
	onOpen: () => void;
	onRemove: () => Promise<void>;
	repository: KnownGitRepository;
}) {
	const [confirmingRemove, setConfirmingRemove] = useState(false);
	const [isRemoving, setIsRemoving] = useState(false);
	const [removeError, setRemoveError] = useState<string | null>(null);
	const path = repository.path ?? "";
	const label = repository.name || pathTail(path) || "Repository";
	const remove = async () => {
		setIsRemoving(true);
		setRemoveError(null);
		try {
			await onRemove();
			setConfirmingRemove(false);
		} catch (error) {
			setRemoveError(errorMessage(error, "Could not remove the repository."));
		} finally {
			setIsRemoving(false);
		}
	};

	return (
		<>
			<ContextMenu>
				<ContextMenuTrigger className="block w-full">
					<RepositoryButton {...{ label, onOpen, path }} />
				</ContextMenuTrigger>
				<ContextMenuContent>
					<ContextMenuGroup>
						<ContextMenuLabel className="max-w-72 truncate font-mono">
							{label}
						</ContextMenuLabel>
					</ContextMenuGroup>
					<ContextMenuSeparator />
					<ContextMenuItem onClick={onOpen}>
						<FolderGit2 aria-hidden="true" /> Open in LovelyGit
					</ContextMenuItem>
					<ContextMenuItem
						onClick={() => void revealKnownRepository(repository.id)}
					>
						<FolderOpen aria-hidden="true" /> Show in File Explorer
					</ContextMenuItem>
					<ContextMenuSeparator />
					<ContextMenuItem
						className="text-destructive focus:bg-destructive/10 focus:text-destructive"
						onClick={() => setConfirmingRemove(true)}
					>
						<Trash2 aria-hidden="true" /> Remove from LovelyGit…
					</ContextMenuItem>
				</ContextMenuContent>
			</ContextMenu>
			<AlertDialog
				onOpenChange={(open) => !isRemoving && setConfirmingRemove(open)}
				open={confirmingRemove}
			>
				<AlertDialogContent>
					<AlertDialogHeader>
						<AlertDialogMedia className="bg-destructive/10 text-destructive">
							<Trash2 aria-hidden="true" />
						</AlertDialogMedia>
						<AlertDialogTitle>Remove {label} from LovelyGit?</AlertDialogTitle>
						<AlertDialogDescription>
							This closes its LovelyGit tab and removes it from recent
							repositories. Files on disk are not changed.
						</AlertDialogDescription>
					</AlertDialogHeader>
					<code className="break-all rounded-md border bg-card p-3 text-xs">
						{path}
					</code>
					{removeError ? (
						<p className="text-destructive text-sm">{removeError}</p>
					) : null}
					<AlertDialogFooter>
						<AlertDialogCancel disabled={isRemoving}>
							Keep repository
						</AlertDialogCancel>
						<AlertDialogAction
							disabled={isRemoving}
							onClick={() => void remove()}
							variant="destructive"
						>
							{isRemoving ? (
								<LoaderCircle className="animate-spin" />
							) : (
								<Trash2 />
							)}
							{isRemoving ? "Removing" : "Remove from LovelyGit"}
						</AlertDialogAction>
					</AlertDialogFooter>
				</AlertDialogContent>
			</AlertDialog>
		</>
	);
}

function RepositoryButton({
	label,
	onOpen,
	path,
}: {
	label: string;
	onOpen: () => void;
	path: string;
}) {
	return (
		<Button
			className="h-auto w-full min-w-0 justify-start px-3 py-2"
			onClick={onOpen}
			title={path || label}
			type="button"
			variant="ghost"
		>
			<span className="grid min-w-0 text-left">
				<span className="truncate font-medium">{label}</span>
				<span className="truncate font-mono text-muted-foreground text-xs">
					{path}
				</span>
			</span>
		</Button>
	);
}

function pathTail(path: string) {
	const normalized = path.replaceAll("\\", "/").replace(/\/+$/, "");
	return normalized.slice(normalized.lastIndexOf("/") + 1);
}

function errorMessage(error: unknown, fallback: string) {
	return error instanceof Error ? error.message : fallback;
}
