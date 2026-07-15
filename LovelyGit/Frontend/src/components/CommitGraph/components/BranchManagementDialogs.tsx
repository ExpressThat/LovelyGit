import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import type { BranchMutationController } from "../hooks/useBranchMutations";
import {
	DeferredCheckoutRemoteBranchDialog,
	DeferredDeleteRemoteBranchDialog,
} from "./DeferredRemoteBranchDialogs";
import {
	LazyBranchComparisonDialog,
	LazyBranchUpstreamDialog,
	LazyDeleteBranchDialog,
	LazyRenameBranchDialog,
} from "./LazyGraphManagementDialogs";

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
				<LazyBranchComparisonDialog
					currentBranchName={currentBranchName}
					onClose={() => controller.setComparisonBranchName(null)}
					onIntegrate={onIntegrateBranch}
					repositoryId={repositoryId}
					targetBranchName={controller.comparisonBranchName}
				/>
			) : null}
			{controller.renameBranchName ? (
				<LazyRenameBranchDialog
					branchName={controller.renameBranchName}
					existingBranchNames={branchNames}
					isBusy={controller.busyBranch !== null}
					key={controller.renameBranchName}
					onConfirm={(name) => void controller.renameBranch(name)}
					onOpenChange={controller.setRenameBranchName}
				/>
			) : null}
			{controller.deleteBranchName ? (
				<LazyDeleteBranchDialog
					branchName={controller.deleteBranchName}
					isBusy={controller.busyBranch !== null}
					key={controller.deleteBranchName}
					onConfirm={(force) => void controller.deleteBranch(force)}
					onOpenChange={controller.setDeleteBranchName}
				/>
			) : null}
			{controller.upstreamBranchName ? (
				<LazyBranchUpstreamDialog
					branchName={controller.upstreamBranchName}
					currentUpstream={upstreams[controller.upstreamBranchName] ?? null}
					isBusy={controller.busyBranch !== null}
					onConfirm={(upstream) => void controller.manageUpstream(upstream)}
					onOpenChange={controller.setUpstreamBranchName}
					remoteBranches={remoteBranches}
				/>
			) : null}
			{controller.checkoutRemoteBranchName ? (
				<DeferredCheckoutRemoteBranchDialog
					existingBranchNames={branchNames}
					isBusy={controller.busyBranch !== null}
					onConfirm={(name) => void controller.checkoutRemoteBranch(name)}
					onOpenChange={controller.setCheckoutRemoteBranchName}
					remoteBranchName={controller.checkoutRemoteBranchName}
				/>
			) : null}
			{controller.deleteRemoteBranchName ? (
				<DeferredDeleteRemoteBranchDialog
					isBusy={controller.busyBranch !== null}
					onConfirm={() => void controller.deleteRemoteBranch()}
					onOpenChange={controller.setDeleteRemoteBranchName}
					remoteBranchName={controller.deleteRemoteBranchName}
				/>
			) : null}
		</>
	);
}
