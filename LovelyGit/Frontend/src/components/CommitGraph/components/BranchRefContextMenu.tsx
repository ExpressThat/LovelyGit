import type { ReactElement } from "react";
import { useState } from "react";
import {
	ContextMenu,
	ContextMenuContent,
	ContextMenuGroup,
	ContextMenuLabel,
	ContextMenuSeparator,
	ContextMenuTrigger,
} from "@/components/ui/context-menu";
import type { CommitRefInfo } from "@/generated/types";
import { BranchUpstreamDialog } from "./BranchUpstreamDialog";
import { CheckoutRemoteBranchDialog } from "./CheckoutRemoteBranchDialog";
import { CheckoutTagDialog } from "./CheckoutTagDialog";
import { CreateBranchFromTagDialog } from "./CreateBranchFromTagDialog";
import { DeleteBranchDialog } from "./DeleteBranchDialog";
import { DeleteTagDialog } from "./DeleteTagDialog";
import { LocalBranchMenuItems } from "./LocalBranchMenuItems";
import { MergeBranchDialog } from "./MergeBranchDialog";
import { PullBranchDialog } from "./PullBranchDialog";
import { PushBranchDialog } from "./PushBranchDialog";
import { PushTagDialog } from "./PushTagDialog";
import { RebaseBranchDialog } from "./RebaseBranchDialog";
import { RemoteBranchMenuItems } from "./RemoteBranchMenuItems";
import { RenameBranchDialog } from "./RenameBranchDialog";
import { TagMenuItems } from "./TagMenuItems";

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
	const [isCheckoutRemoteOpen, setIsCheckoutRemoteOpen] = useState(false);
	const [isCheckoutTagOpen, setIsCheckoutTagOpen] = useState(false);
	const [isCreateBranchFromTagOpen, setIsCreateBranchFromTagOpen] =
		useState(false);
	const [isDeleteOpen, setIsDeleteOpen] = useState(false);
	const [isDeleteTagOpen, setIsDeleteTagOpen] = useState(false);
	const [isMergeOpen, setIsMergeOpen] = useState(false);
	const [isPullOpen, setIsPullOpen] = useState(false);
	const [isPushOpen, setIsPushOpen] = useState(false);
	const [isPushTagOpen, setIsPushTagOpen] = useState(false);
	const [isRebaseOpen, setIsRebaseOpen] = useState(false);
	const [isRenameOpen, setIsRenameOpen] = useState(false);
	const [isUpstreamOpen, setIsUpstreamOpen] = useState(false);
	const isLocalBranch = refInfo.kind === "Local";
	const isRemoteBranch = refInfo.kind === "Remote";
	const isTag = refInfo.kind === "Tag";
	const isCurrentBranch = refInfo.name === currentBranchName;
	const canMutateBranch =
		repositoryId !== null && isLocalBranch && !isCurrentBranch;
	const canPullBranch =
		repositoryId !== null && isLocalBranch && isCurrentBranch;
	const canManageUpstream = repositoryId !== null && isLocalBranch;
	const canCheckoutRemote = repositoryId !== null && isRemoteBranch;
	const canCheckoutTag = repositoryId !== null && isTag;
	const canCreateBranchFromTag = repositoryId !== null && isTag;
	const canPushBranch = repositoryId !== null && isLocalBranch;
	const canDeleteTag = repositoryId !== null && isTag;
	const canPushTag = repositoryId !== null && isTag;

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
					{isRemoteBranch ? (
						<RemoteBranchMenuItems
							canCheckoutRemote={canCheckoutRemote}
							onCheckout={() => setIsCheckoutRemoteOpen(true)}
							remoteBranchName={refInfo.name}
						/>
					) : null}
					{isTag ? (
						<TagMenuItems
							canCheckoutTag={canCheckoutTag}
							canCreateBranch={canCreateBranchFromTag}
							canDeleteTag={canDeleteTag}
							canPushTag={canPushTag}
							onCheckout={() => setIsCheckoutTagOpen(true)}
							onCreateBranch={() => setIsCreateBranchFromTagOpen(true)}
							onDelete={() => setIsDeleteTagOpen(true)}
							onPush={() => setIsPushTagOpen(true)}
							onPushSuccess={onRefsChanged}
							repositoryId={repositoryId}
							tagName={refInfo.name}
							tagRemoteUrl={refInfo.remoteUrl}
						/>
					) : null}
					{isLocalBranch ? (
						<LocalBranchMenuItems
							branchName={refInfo.name}
							canManageUpstream={canManageUpstream}
							canMutateBranch={canMutateBranch}
							canPullBranch={canPullBranch}
							canPushBranch={canPushBranch}
							isCurrentBranch={isCurrentBranch}
							onCheckoutSuccess={onRefsChanged}
							onDelete={(force) => {
								setDeleteForce(force);
								setIsDeleteOpen(true);
							}}
							onMerge={() => setIsMergeOpen(true)}
							onPull={() => setIsPullOpen(true)}
							onPush={() => setIsPushOpen(true)}
							onRebase={() => setIsRebaseOpen(true)}
							onRename={() => setIsRenameOpen(true)}
							onUpstream={() => setIsUpstreamOpen(true)}
							repositoryId={repositoryId}
						/>
					) : null}
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
			<DeleteTagDialog
				isOpen={isDeleteTagOpen}
				onOpenChange={setIsDeleteTagOpen}
				onSuccess={onRefsChanged}
				repositoryId={repositoryId}
				tagName={refInfo.name}
			/>
			<PushTagDialog
				isOpen={isPushTagOpen}
				onOpenChange={setIsPushTagOpen}
				onSuccess={onRefsChanged}
				repositoryId={repositoryId}
				tagName={refInfo.name}
			/>
			<CheckoutTagDialog
				isOpen={isCheckoutTagOpen}
				onOpenChange={setIsCheckoutTagOpen}
				onSuccess={onRefsChanged}
				repositoryId={repositoryId}
				tagName={refInfo.name}
			/>
			<CreateBranchFromTagDialog
				isOpen={isCreateBranchFromTagOpen}
				onOpenChange={setIsCreateBranchFromTagOpen}
				onSuccess={onRefsChanged}
				repositoryId={repositoryId}
				tagName={refInfo.name}
			/>
			<CheckoutRemoteBranchDialog
				isOpen={isCheckoutRemoteOpen}
				onOpenChange={setIsCheckoutRemoteOpen}
				onSuccess={onRefsChanged}
				remoteBranchName={refInfo.name}
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
