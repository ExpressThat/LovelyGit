import { ArchiveRestore, Copy, Trash2 } from "lucide-react";
import type { ReactElement } from "react";
import { useState } from "react";
import { toast } from "sonner";
import {
	AlertDialog,
	AlertDialogAction,
	AlertDialogCancel,
	AlertDialogContent,
	AlertDialogDescription,
	AlertDialogFooter,
	AlertDialogHeader,
	AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import {
	ContextMenu,
	ContextMenuContent,
	ContextMenuGroup,
	ContextMenuItem,
	ContextMenuLabel,
	ContextMenuSeparator,
	ContextMenuTrigger,
} from "@/components/ui/context-menu";
import type { CommitRefInfo } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { copyToClipboard } from "../utils/clipboard";

const stashActionTimeoutMs = 120_000;

export function StashRefContextMenu({
	children,
	onRefsChanged,
	refInfo,
	repositoryId,
}: {
	children: ReactElement;
	onRefsChanged: () => void;
	refInfo: CommitRefInfo;
	repositoryId: string | null;
}) {
	const [isDeleteOpen, setIsDeleteOpen] = useState(false);
	const canRunStashAction = repositoryId !== null;

	return (
		<>
			<ContextMenu>
				<ContextMenuTrigger onContextMenu={(event) => event.stopPropagation()}>
					{children}
				</ContextMenuTrigger>
				<ContextMenuContent className="w-52">
					<ContextMenuGroup>
						<ContextMenuLabel className="truncate">
							{refInfo.name}
						</ContextMenuLabel>
					</ContextMenuGroup>
					<ContextMenuSeparator />
					{canRunStashAction ? (
						<>
							<ContextMenuItem
								onClick={() => runStashAction("apply", refInfo.name)}
							>
								<ArchiveRestore />
								Apply stash
							</ContextMenuItem>
							<ContextMenuItem
								onClick={() => runStashAction("pop", refInfo.name)}
							>
								<ArchiveRestore />
								Pop stash
							</ContextMenuItem>
						</>
					) : null}
					<ContextMenuItem
						onClick={() => void copyToClipboard(refInfo.name, "Stash name")}
					>
						<Copy />
						Copy stash name
					</ContextMenuItem>
					{canRunStashAction ? (
						<>
							<ContextMenuSeparator />
							<ContextMenuItem
								onClick={() => setIsDeleteOpen(true)}
								variant="destructive"
							>
								<Trash2 />
								Delete stash
							</ContextMenuItem>
						</>
					) : null}
				</ContextMenuContent>
			</ContextMenu>
			<AlertDialog onOpenChange={setIsDeleteOpen} open={isDeleteOpen}>
				<AlertDialogContent>
					<AlertDialogHeader>
						<AlertDialogTitle>Delete stash</AlertDialogTitle>
						<AlertDialogDescription>
							Delete {refInfo.name}. This removes the saved stash entry and
							cannot be undone from LovelyGit yet.
						</AlertDialogDescription>
					</AlertDialogHeader>
					<AlertDialogFooter>
						<AlertDialogCancel>Cancel</AlertDialogCancel>
						<AlertDialogAction
							onClick={() => runStashAction("drop", refInfo.name)}
						>
							Delete stash
						</AlertDialogAction>
					</AlertDialogFooter>
				</AlertDialogContent>
			</AlertDialog>
		</>
	);

	async function runStashAction(
		action: "apply" | "drop" | "pop",
		stashName: string,
	) {
		if (!repositoryId) {
			return;
		}

		const commandType =
			action === "apply"
				? NativeMessageType.ApplyStash
				: action === "pop"
					? NativeMessageType.PopStash
					: NativeMessageType.DropStash;
		const label =
			action === "apply"
				? "Applied stash"
				: action === "pop"
					? "Popped stash"
					: "Deleted stash";
		try {
			await sendRequestWithResponse(
				{
					arguments: { repositoryId, stashName },
					commandType,
				},
				{ timeoutMs: stashActionTimeoutMs },
			);
			toast.success(label);
			onRefsChanged();
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Stash action failed",
			);
		}
	}
}
