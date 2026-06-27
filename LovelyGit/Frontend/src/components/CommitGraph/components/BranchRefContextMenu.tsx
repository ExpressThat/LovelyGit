import {
	GitBranch,
	GitPullRequestArrow,
	Pencil,
	RefreshCw,
	Trash2,
	Upload,
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
import { BranchUpstreamDialog } from "./BranchUpstreamDialog";
import { DeleteBranchDialog } from "./DeleteBranchDialog";
import { MergeBranchDialog } from "./MergeBranchDialog";
import { PullBranchDialog } from "./PullBranchDialog";
import { PushBranchDialog } from "./PushBranchDialog";
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
	const [isPullOpen, setIsPullOpen] = useState(false);
	const [isPushOpen, setIsPushOpen] = useState(false);
	const [isRebaseOpen, setIsRebaseOpen] = useState(false);
	const [isRenameOpen, setIsRenameOpen] = useState(false);
	const [isUpstreamOpen, setIsUpstreamOpen] = useState(false);
	const isLocalBranch = refInfo.kind === "Local";
	const isCurrentBranch = refInfo.name === currentBranchName;
	const canCheckout =
		repositoryId !== null && isLocalBranch && !isCurrentBranch;
	const canMutateBranch =
		repositoryId !== null && isLocalBranch && !isCurrentBranch;
	const canPullBranch =
		repositoryId !== null && isLocalBranch && isCurrentBranch;
	const canManageUpstream = repositoryId !== null && isLocalBranch;

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
					<ContextMenuItem
						disabled={!canManageUpstream}
						onClick={() => setIsUpstreamOpen(true)}
					>
						<GitBranch />
						Upstream settings
					</ContextMenuItem>
					<ContextMenuItem
						disabled={!canPullBranch}
						onClick={() => setIsPullOpen(true)}
					>
						<RefreshCw />
						Pull branch
					</ContextMenuItem>
					<ContextMenuItem
						disabled={!isLocalBranch || repositoryId === null}
						onClick={() => setIsPushOpen(true)}
					>
						<Upload />
						Push branch
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
			<BranchUpstreamDialog
				branchName={refInfo.name}
				isOpen={isUpstreamOpen}
				onOpenChange={setIsUpstreamOpen}
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
			<PullBranchDialog
				branchName={refInfo.name}
				isOpen={isPullOpen}
				onOpenChange={setIsPullOpen}
				onSuccess={onRefsChanged}
				repositoryId={repositoryId}
			/>
			<PushBranchDialog
				branchName={refInfo.name}
				isOpen={isPushOpen}
				onOpenChange={setIsPushOpen}
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
