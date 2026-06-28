import {
	Copy,
	GitBranch,
	GitPullRequestArrow,
	Pencil,
	RefreshCw,
	Trash2,
	Upload,
} from "lucide-react";
import { toast } from "sonner";
import { ContextMenuItem } from "@/components/ui/context-menu";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { copyToClipboard } from "../utils/clipboard";

export function LocalBranchMenuItems({
	branchName,
	canManageUpstream,
	canMutateBranch,
	canPullBranch,
	canPushBranch,
	isCurrentBranch,
	onCheckoutSuccess,
	onDelete,
	onMerge,
	onPull,
	onPush,
	onRebase,
	onRename,
	onUpstream,
	repositoryId,
}: {
	branchName: string;
	canManageUpstream: boolean;
	canMutateBranch: boolean;
	canPullBranch: boolean;
	canPushBranch: boolean;
	isCurrentBranch: boolean;
	onCheckoutSuccess: () => void;
	onDelete: (force: boolean) => void;
	onMerge: () => void;
	onPull: () => void;
	onPush: () => void;
	onRebase: () => void;
	onRename: () => void;
	onUpstream: () => void;
	repositoryId: string | null;
}) {
	return (
		<>
			{!isCurrentBranch ? (
				<ContextMenuItem onClick={() => checkoutBranch()}>
					<GitBranch />
					Checkout branch
				</ContextMenuItem>
			) : null}
			<ContextMenuItem
				onClick={() => void copyToClipboard(branchName, "Branch name")}
			>
				<Copy />
				Copy branch name
			</ContextMenuItem>
			{canMutateBranch ? (
				<ContextMenuItem onClick={onRename}>
					<Pencil />
					Rename branch
				</ContextMenuItem>
			) : null}
			{canManageUpstream ? (
				<ContextMenuItem onClick={onUpstream}>
					<GitBranch />
					Upstream settings
				</ContextMenuItem>
			) : null}
			{canPullBranch ? (
				<ContextMenuItem onClick={onPull}>
					<RefreshCw />
					Pull branch
				</ContextMenuItem>
			) : null}
			{canPushBranch ? (
				<ContextMenuItem onClick={onPush}>
					<Upload />
					Push branch
				</ContextMenuItem>
			) : null}
			{canMutateBranch ? (
				<>
					<ContextMenuItem onClick={onMerge}>
						<GitPullRequestArrow />
						Merge into current
					</ContextMenuItem>
					<ContextMenuItem onClick={onRebase}>
						<GitPullRequestArrow />
						Rebase current onto branch
					</ContextMenuItem>
					<ContextMenuItem
						onClick={() => onDelete(false)}
						variant="destructive"
					>
						<Trash2 />
						Delete branch
					</ContextMenuItem>
					<ContextMenuItem onClick={() => onDelete(true)} variant="destructive">
						<Trash2 />
						Force delete branch
					</ContextMenuItem>
				</>
			) : null}
		</>
	);

	async function checkoutBranch() {
		if (repositoryId === null) {
			return;
		}

		try {
			await sendRequestWithResponse({
				arguments: {
					branchName,
					repositoryId,
				},
				commandType: NativeMessageType.CheckoutBranch,
			});
			toast.success(`Checked out ${branchName}`);
			onCheckoutSuccess();
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not checkout branch",
			);
		}
	}
}
