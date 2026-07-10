import type { BranchMutationController } from "../hooks/useBranchMutations";
import { BranchUpstreamDialog } from "./BranchUpstreamDialog";
import { DeleteBranchDialog } from "./DeleteBranchDialog";
import { RenameBranchDialog } from "./RenameBranchDialog";

export function BranchManagementDialogs({
	branchNames,
	controller,
	remoteBranches,
	upstreams,
}: {
	branchNames: string[];
	controller: BranchMutationController;
	remoteBranches: string[];
	upstreams: Record<string, string>;
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
			{controller.upstreamBranchName ? (
				<BranchUpstreamDialog
					branchName={controller.upstreamBranchName}
					currentUpstream={upstreams[controller.upstreamBranchName] ?? null}
					isBusy={controller.busyBranch !== null}
					onConfirm={(upstream) => void controller.manageUpstream(upstream)}
					onOpenChange={controller.setUpstreamBranchName}
					remoteBranches={remoteBranches}
				/>
			) : null}
		</>
	);
}
