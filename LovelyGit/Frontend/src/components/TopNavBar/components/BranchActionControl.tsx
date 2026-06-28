import { GitBranch } from "lucide-react";
import { useState } from "react";
import { CreateBranchFromCommitDialog } from "@/components/CommitGraph/components/CreateBranchFromCommitDialog";
import { Button } from "@/components/ui/button";

export function BranchActionControl({
	onBranchCreated,
	repositoryId,
	selectedCommitHash,
}: {
	onBranchCreated: () => void;
	repositoryId: string | null;
	selectedCommitHash: string | null;
}) {
	const [isCreateBranchOpen, setIsCreateBranchOpen] = useState(false);
	const canCreateBranch = repositoryId !== null && selectedCommitHash !== null;

	return (
		<>
			<Button
				aria-label="Create branch at selected commit"
				className="h-8"
				disabled={!canCreateBranch}
				onClick={() => setIsCreateBranchOpen(true)}
				size="sm"
				title={
					canCreateBranch
						? "Create branch at selected commit"
						: "Select a commit to create a branch"
				}
				type="button"
				variant="ghost"
			>
				<GitBranch aria-hidden="true" />
				<span>Branch</span>
			</Button>
			{selectedCommitHash ? (
				<CreateBranchFromCommitDialog
					commitHash={selectedCommitHash}
					isOpen={isCreateBranchOpen}
					onOpenChange={setIsCreateBranchOpen}
					onSuccess={onBranchCreated}
					repositoryId={repositoryId}
				/>
			) : null}
		</>
	);
}
