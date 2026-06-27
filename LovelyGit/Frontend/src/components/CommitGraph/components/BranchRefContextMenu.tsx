import {
	GitBranch,
	GitPullRequestArrow,
	Pencil,
	RefreshCw,
	Trash2,
} from "lucide-react";
import type { ReactElement } from "react";
import { useState } from "react";
import { toast } from "sonner";
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
import { DeleteBranchDialog } from "./DeleteBranchDialog";
import { MergeBranchDialog } from "./MergeBranchDialog";
import { RebaseBranchDialog } from "./RebaseBranchDialog";
import { RenameBranchDialog } from "./RenameBranchDialog";

export function BranchRefContextMenu({
	children,
	currentBranchName,
	onRefsChanged,
	refInfo,
	repositoryId,
}: {
	children: ReactElement;
	currentBranchName: string | null;
	onRefsChanged: () => void;
	refInfo: CommitRefInfo;
	repositoryId: string | null;
}) {
	const [deleteForce, setDeleteForce] = useState(false);
	const [isDeleteOpen, setIsDeleteOpen] = useState(false);
	const [isMergeOpen, setIsMergeOpen] = useState(false);
	const [isRebaseOpen, setIsRebaseOpen] = useState(false);
	const [isRenameOpen, setIsRenameOpen] = useState(false);
	const isLocalBranch = refInfo.kind === "Local";
	const isCurrentBranch = refInfo.name === currentBranchName;
	const canCheckout =
		repositoryId !== null && isLocalBranch && !isCurrentBranch;
	const canMutateBranch =
		repositoryId !== null && isLocalBranch && !isCurrentBranch;

	const checkoutBranch = async () => {
		if (!canCheckout || repositoryId === null) {
			return;
		}

		try {
			await sendRequestWithResponse({
				arguments: {
					branchName: refInfo.name,
					repositoryId,
				},
				commandType: NativeMessageType.CheckoutBranch,
			});
			toast.success(`Checked out ${refInfo.name}`);
			onRefsChanged();
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not checkout branch",
			);
		}
	};

	return (
		<>
			<ContextMenu>
				<ContextMenuTrigger
					onContextMenu={(event) => event.stopPropagation()}
					render={children}
				/>
				<ContextMenuContent className="w-56">
					<ContextMenuGroup>
						<ContextMenuLabel className="truncate">
							{refInfo.name}
						</ContextMenuLabel>
					</ContextMenuGroup>
					<ContextMenuSeparator />
					<ContextMenuItem disabled={!canCheckout} onClick={checkoutBranch}>
						<GitBranch />
						Checkout branch
					</ContextMenuItem>
					<ContextMenuItem
						disabled={!canMutateBranch}
						onClick={() => setIsRenameOpen(true)}
					>
						<Pencil />
						Rename branch
					</ContextMenuItem>
					<ContextMenuItem disabled>
						<RefreshCw />
						Pull branch
					</ContextMenuItem>
					<ContextMenuItem
						disabled={!canMutateBranch}
						onClick={() => setIsMergeOpen(true)}
					>
						<GitPullRequestArrow />
						Merge into current
					</ContextMenuItem>
					<ContextMenuItem
						disabled={!canMutateBranch}
						onClick={() => setIsRebaseOpen(true)}
					>
						<GitPullRequestArrow />
						Rebase current onto branch
					</ContextMenuItem>
					<ContextMenuItem
						disabled={!canMutateBranch}
						onClick={() => {
							setDeleteForce(false);
							setIsDeleteOpen(true);
						}}
						variant="destructive"
					>
						<Trash2 />
						Delete branch
					</ContextMenuItem>
					<ContextMenuItem
						disabled={!canMutateBranch}
						onClick={() => {
							setDeleteForce(true);
							setIsDeleteOpen(true);
						}}
						variant="destructive"
					>
						<Trash2 />
						Force delete branch
					</ContextMenuItem>
				</ContextMenuContent>
			</ContextMenu>
			<DeleteBranchDialog
				branchName={refInfo.name}
				force={deleteForce}
				isOpen={isDeleteOpen}
				onOpenChange={setIsDeleteOpen}
				onSuccess={onRefsChanged}
				repositoryId={repositoryId}
			/>
			<MergeBranchDialog
				branchName={refInfo.name}
				currentBranchName={currentBranchName}
				isOpen={isMergeOpen}
				onOpenChange={setIsMergeOpen}
				onSuccess={onRefsChanged}
				repositoryId={repositoryId}
			/>
			<RebaseBranchDialog
				branchName={refInfo.name}
				currentBranchName={currentBranchName}
				isOpen={isRebaseOpen}
				onOpenChange={setIsRebaseOpen}
				onSuccess={onRefsChanged}
				repositoryId={repositoryId}
			/>
			<RenameBranchDialog
				branchName={refInfo.name}
				isOpen={isRenameOpen}
				onOpenChange={setIsRenameOpen}
				onSuccess={onRefsChanged}
				repositoryId={repositoryId}
			/>
		</>
	);
}
