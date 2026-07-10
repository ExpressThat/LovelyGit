import type { BranchMutationController } from "../hooks/useBranchMutations";
import { DeleteBranchDialog } from "./DeleteBranchDialog";
import { RenameBranchDialog } from "./RenameBranchDialog";

export function BranchManagementDialogs({
	branchNames,
	controller,
}: {
	branchNames: string[];
	controller: BranchMutationController;
}) {
	return (
		<>
			{controller.renameBranchName ? (
				<RenameBranchDialog
					branchName={controller.renameBranchName}
					existingBranchNames={branchNames}
					isBusy={controller.busyBranch !== null}
					key={controller.renameBranchName}
					onConfirm={(name) => void controller.renameBranch(name)}
					onOpenChange={controller.setRenameBranchName}
				/>
			) : null}
			{controller.deleteBranchName ? (
				<DeleteBranchDialog
					branchName={controller.deleteBranchName}
					isBusy={controller.busyBranch !== null}
					key={controller.deleteBranchName}
					onConfirm={(force) => void controller.deleteBranch(force)}
					onOpenChange={controller.setDeleteBranchName}
				/>
			) : null}
		</>
	);
}
