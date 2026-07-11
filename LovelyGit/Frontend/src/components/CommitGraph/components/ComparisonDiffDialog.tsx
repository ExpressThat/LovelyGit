import { CommitFileDiffView } from "@/components/CommitFileDiff/CommitFileDiffView";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogTitle,
} from "@/components/ui/dialog";
import type {
	BranchComparisonFile,
	CommitChangedFile,
} from "@/generated/types";

export function ComparisonDiffDialog({
	baseCommitHash,
	file,
	onClose,
	repositoryId,
	targetCommitHash,
}: {
	baseCommitHash: string;
	file: BranchComparisonFile;
	onClose: () => void;
	repositoryId: string;
	targetCommitHash: string;
}) {
	return (
		<Dialog onOpenChange={(open) => !open && onClose()} open>
			<DialogContent className="h-[min(86vh,820px)] overflow-hidden p-0 sm:max-w-[min(92vw,1280px)]">
				<DialogTitle className="sr-only">
					Comparison diff for {file.path}
				</DialogTitle>
				<DialogDescription className="sr-only">
					Changes between the selected comparison commits.
				</DialogDescription>
				<CommitFileDiffView
					commitHash={targetCommitHash}
					comparisonCommitHash={baseCommitHash}
					file={toChangedFile(file)}
					onClose={onClose}
					parentIndex={0}
					repositoryId={repositoryId}
					showFileStats={false}
				/>
			</DialogContent>
		</Dialog>
	);
}

function toChangedFile(file: BranchComparisonFile): CommitChangedFile {
	return {
		additions: 0,
		deletions: 0,
		isBinary: false,
		path: file.path,
		status: file.status,
	};
}
