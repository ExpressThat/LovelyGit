import { lazy, Suspense, useEffect, useState } from "react";
import { SurfaceLoading } from "./AppLazySurfaces";
import type { FileBlameTarget } from "./components/FileBlame/FileBlameDialog";
import type { FileHistoryTarget } from "./components/FileHistory/FileHistoryDialog";
import { Toaster } from "./components/ui/sonner";

const CommitSearchDialog = lazy(() =>
	import("./components/CommitSearch/CommitSearchDialog").then((module) => ({
		default: module.CommitSearchDialog,
	})),
);
const FileHistoryDialog = lazy(() =>
	import("./components/FileHistory/FileHistoryDialog").then((module) => ({
		default: module.FileHistoryDialog,
	})),
);
const FileBlameDialog = lazy(() =>
	import("./components/FileBlame/FileBlameDialog").then((module) => ({
		default: module.FileBlameDialog,
	})),
);

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
	const retainSearch = useRetainedSurface(isCommitSearchOpen);
	const retainHistory = useRetainedSurface(fileHistoryTarget !== null);
	const retainBlame = useRetainedSurface(fileBlameTarget !== null);
	return (
		<>
			<Suspense fallback={<SurfaceLoading label="Opening tool" overlay />}>
				{retainSearch ? (
					<CommitSearchDialog
						onOpenChange={onSearchOpenChange}
						onSelectCommit={onSelectCommit}
						open={isCommitSearchOpen && Boolean(repositoryId)}
						repositoryId={repositoryId}
					/>
				) : null}
				{retainHistory ? (
					<FileHistoryDialog
						onOpenChange={onFileHistoryOpenChange}
						onSelectCommit={onSelectCommit}
						repositoryId={repositoryId}
						target={fileHistoryTarget}
					/>
				) : null}
				{retainBlame ? (
					<FileBlameDialog
						onOpenChange={onFileBlameOpenChange}
						onSelectCommit={onSelectCommit}
						repositoryId={repositoryId}
						target={fileBlameTarget}
					/>
				) : null}
			</Suspense>
			<Toaster />
		</>
	);
}

function useRetainedSurface(active: boolean) {
	const [retained, setRetained] = useState(active);
	useEffect(() => {
		if (active) setRetained(true);
	}, [active]);
	return retained;
}
