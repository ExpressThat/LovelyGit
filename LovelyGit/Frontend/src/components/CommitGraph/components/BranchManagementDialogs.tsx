import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import type { BranchMutationController } from "../hooks/useBranchMutations";
import { BranchComparisonDialog } from "./BranchComparisonDialog";
import { BranchUpstreamDialog } from "./BranchUpstreamDialog";
import { DeleteBranchDialog } from "./DeleteBranchDialog";
import { RenameBranchDialog } from "./RenameBranchDialog";

export function BranchManagementDialogs({
	branchNames,
	controller,
	currentBranchName,
	onIntegrateBranch,
	repositoryId,
	remoteBranches,
	upstreams,
}: {
	branchNames: string[];
	controller: BranchMutationController;
	currentBranchName: string | null;
	onIntegrateBranch: (mode: BranchIntegrationMode, branchName: string) => void;
	repositoryId: string | null;
	remoteBranches: string[];
	upstreams: Record<string, string>;
}) {
	return (
		<>
			{controller.comparisonBranchName ? (
				<BranchComparisonDialog
					currentBranchName={currentBranchName}
					onClose={() => controller.setComparisonBranchName(null)}
					onIntegrate={onIntegrateBranch}
					repositoryId={repositoryId}
					targetBranchName={controller.comparisonBranchName}
				/>
			) : null}
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
