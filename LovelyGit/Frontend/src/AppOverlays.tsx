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
const CommandPalette = lazy(() =>
	import("./components/CommandPalette/CommandPalette").then((module) => ({
		default: module.CommandPalette,
	})),
);

export function AppOverlays({
	fileHistoryTarget,
	fileBlameTarget,
	isCommitSearchOpen,
	isCommandPaletteOpen,
	onFileHistoryOpenChange,
	onFileBlameOpenChange,
	onSearchOpenChange,
	onCommandPaletteOpenChange,
	onOpenSettings,
	onOpenWorkingChanges,
	onRefreshRepository,
	onSelectCommit,
	repositoryId,
}: {
	fileHistoryTarget: FileHistoryTarget | null;
	fileBlameTarget: FileBlameTarget | null;
	isCommitSearchOpen: boolean;
	isCommandPaletteOpen: boolean;
	onFileHistoryOpenChange: (open: boolean) => void;
	onFileBlameOpenChange: (open: boolean) => void;
	onSearchOpenChange: (open: boolean) => void;
	onCommandPaletteOpenChange: (open: boolean) => void;
	onOpenSettings: () => void;
	onOpenWorkingChanges: () => void;
	onRefreshRepository: () => void | Promise<void>;
	onSelectCommit: (commitHash: string) => void;
	repositoryId: string | null;
}) {
	const retainSearch = useRetainedSurface(isCommitSearchOpen);
	const retainHistory = useRetainedSurface(fileHistoryTarget !== null);
	const retainBlame = useRetainedSurface(fileBlameTarget !== null);
	const retainPalette = useRetainedSurface(isCommandPaletteOpen);
	return (
		<>
			<Suspense fallback={<SurfaceLoading label="Opening tool" overlay />}>
				{retainPalette ? (
					<CommandPalette
						onOpenChange={onCommandPaletteOpenChange}
						onOpenCommitSearch={() => onSearchOpenChange(true)}
						onOpenSettings={onOpenSettings}
						onOpenWorkingChanges={onOpenWorkingChanges}
						onRefreshRepository={onRefreshRepository}
						open={isCommandPaletteOpen}
					/>
				) : null}
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
