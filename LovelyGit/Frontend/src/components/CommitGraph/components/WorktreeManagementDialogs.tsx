import type { RepositoryWorktreeItem } from "@/generated/types";
import type { WorktreeMutationController } from "../hooks/useWorktreeMutations";
import { CreateWorktreeDialog } from "./CreateWorktreeDialog";
import { LockWorktreeDialog } from "./LockWorktreeDialog";
import { RemoveWorktreeDialog } from "./RemoveWorktreeDialog";

export function WorktreeManagementDialogs({
	controller,
	worktrees,
}: {
	controller: WorktreeMutationController;
	worktrees: RepositoryWorktreeItem[];
}) {
	const existingWorktree = controller.createBranchName
		? (worktrees.find(
				(worktree) => worktree.branchName === controller.createBranchName,
			) ?? null)
		: null;
	const lockTarget = controller.lockTarget;
	const removeTarget = controller.removeTarget;
	return (
		<>
			{controller.createBranchName ? (
				<CreateWorktreeDialog
					branchName={controller.createBranchName}
					existingWorktree={existingWorktree}
					isBusy={controller.busyPath !== null}
					onChooseDestination={async () =>
						(await controller.chooseDestination())?.path ?? null
					}
					onClose={() => controller.setCreateBranchName(null)}
					onCreate={(path) => void controller.create(path)}
					onOpenExisting={(worktree) => controller.manage("Open", worktree)}
				/>
			) : null}
			{lockTarget ? (
				<LockWorktreeDialog
					isBusy={controller.busyPath !== null}
					onClose={() => controller.setLockTarget(null)}
					onConfirm={(lockReason) =>
						void controller.mutate("Lock", lockTarget, {
							lockReason,
						})
					}
					worktree={lockTarget}
				/>
			) : null}
			{removeTarget ? (
				<RemoveWorktreeDialog
					isBusy={controller.busyPath !== null}
					onClose={() => controller.setRemoveTarget(null)}
					onConfirm={(force) =>
						void controller.mutate("Remove", removeTarget, {
							force,
						})
					}
					worktree={removeTarget}
				/>
			) : null}
		</>
	);
}
