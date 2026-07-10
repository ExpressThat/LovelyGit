import { CommitSearchDialog } from "./components/CommitSearch/CommitSearchDialog";
import {
	FileHistoryDialog,
	type FileHistoryTarget,
} from "./components/FileHistory/FileHistoryDialog";
import { Toaster } from "./components/ui/sonner";

export function AppOverlays({
	fileHistoryTarget,
	isCommitSearchOpen,
	onFileHistoryOpenChange,
	onSearchOpenChange,
	onSelectCommit,
	repositoryId,
}: {
	fileHistoryTarget: FileHistoryTarget | null;
	isCommitSearchOpen: boolean;
	onFileHistoryOpenChange: (open: boolean) => void;
	onSearchOpenChange: (open: boolean) => void;
	onSelectCommit: (commitHash: string) => void;
	repositoryId: string | null;
}) {
	return (
		<>
			<CommitSearchDialog
				onOpenChange={onSearchOpenChange}
				onSelectCommit={onSelectCommit}
				open={isCommitSearchOpen && Boolean(repositoryId)}
				repositoryId={repositoryId}
			/>
			<FileHistoryDialog
				onOpenChange={onFileHistoryOpenChange}
				onSelectCommit={onSelectCommit}
				repositoryId={repositoryId}
				target={fileHistoryTarget}
			/>
			<Toaster />
		</>
	);
}
