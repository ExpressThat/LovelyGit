import { openRemoteWebResource } from "@/components/TopNavBar/components/RepositoryCommands";
import type { BranchAction } from "../components/BranchContextMenu";
import { useBranchMutations } from "./useBranchMutations";
import { useReflogManagement } from "./useReflogManagement";
import { useTagMutations } from "./useTagMutations";
import { useWorktreeMutations } from "./useWorktreeMutations";

export function useBranchWorktreeControllers({
	currentBranchName,
	onCurrentBranchNameChange,
	onRepositoryChanged,
	onUpstreamChanged,
	onWorktreeLockChanged,
	onWorktreeRemoved,
	onWorktreesChanged,
	remoteName,
	repositoryId,
}: {
	currentBranchName: string | null;
	onCurrentBranchNameChange: (branchName: string) => void;
	onRepositoryChanged: () => void;
	onUpstreamChanged: (branchName: string, upstreamName: string | null) => void;
	onWorktreeLockChanged: (
		path: string,
		isLocked: boolean,
		lockReason: string,
	) => void;
	onWorktreeRemoved: (path: string) => void;
	onWorktreesChanged: () => void;
	remoteName: string | null;
	repositoryId: string | null;
}) {
	const branchController = useBranchMutations({
		currentBranchName,
		onCurrentBranchNameChange,
		onRepositoryChanged,
		onUpstreamChanged,
		remoteName,
		repositoryId,
	});
	const worktreeController = useWorktreeMutations({
		onRepositoryChanged: onWorktreesChanged,
		onWorktreeLockChanged,
		onWorktreeRemoved,
		repositoryId,
	});
	const reflogController = useReflogManagement();
	const tagController = useTagMutations({
		onRepositoryChanged,
		remoteName,
		repositoryId,
	});
	const manageBranch = (action: BranchAction, branchName: string) => {
		if (action === "reflog") reflogController.open(branchName);
		else if (
			action === "createPullRequest" &&
			repositoryId &&
			currentBranchName
		)
			void openRemoteWebResource(
				repositoryId,
				"PullRequest",
				branchName,
				currentBranchName,
			);
		else if (action === "openRemote" && repositoryId)
			void openRemoteWebResource(repositoryId, "Branch", branchName);
		else if (action === "worktree")
			worktreeController.setCreateBranchName(branchName);
		else branchController.manageBranch(action, branchName);
	};

	return {
		branchController,
		manageBranch,
		reflogController,
		tagController,
		worktreeController,
	};
}
