import { CommitSearchDialog } from "./components/CommitSearch/CommitSearchDialog";
import {
	FileBlameDialog,
	type FileBlameTarget,
} from "./components/FileBlame/FileBlameDialog";
import {
	FileHistoryDialog,
	type FileHistoryTarget,
} from "./components/FileHistory/FileHistoryDialog";
import { Toaster } from "./components/ui/sonner";

export function AppOverlays({
	fileHistoryTarget,
	fileBlameTarget,
	isCommitSearchOpen,
	onFileHistoryOpenChange,
	onFileBlameOpenChange,
	onSearchOpenChange,
	onSelectCommit,
	repositoryId,
}: {
	fileHistoryTarget: FileHistoryTarget | null;
	fileBlameTarget: FileBlameTarget | null;
	isCommitSearchOpen: boolean;
	onFileHistoryOpenChange: (open: boolean) => void;
	onFileBlameOpenChange: (open: boolean) => void;
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
			<FileBlameDialog
				onOpenChange={onFileBlameOpenChange}
				onSelectCommit={onSelectCommit}
				repositoryId={repositoryId}
				target={fileBlameTarget}
			/>
			<Toaster />
		</>
	);
}
