import type { BranchAction } from "../components/BranchContextMenu";
import { useBranchMutations } from "./useBranchMutations";
import { useWorktreeMutations } from "./useWorktreeMutations";

export function useBranchWorktreeControllers({
	currentBranchName,
	onCurrentBranchNameChange,
	onRepositoryChanged,
	onUpstreamChanged,
	onWorktreesChanged,
	remoteName,
	repositoryId,
}: {
	currentBranchName: string | null;
	onCurrentBranchNameChange: (branchName: string) => void;
	onRepositoryChanged: () => void;
	onUpstreamChanged: (branchName: string, upstreamName: string | null) => void;
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
		repositoryId,
	});
	const manageBranch = (action: BranchAction, branchName: string) => {
		if (action === "worktree")
			worktreeController.setCreateBranchName(branchName);
		else branchController.manageBranch(action, branchName);
	};

	return { branchController, manageBranch, worktreeController };
}
