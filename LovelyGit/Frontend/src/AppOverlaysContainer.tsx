import type { Dispatch, SetStateAction } from "react";
import { AppOverlays } from "./AppOverlays";
import type { DetailsPanelState } from "./AppPanelState";
import type { useAppOverlayState } from "./useAppOverlayState";
import type { useFileDiscoveryTargets } from "./useFileDiscoveryTargets";

export function AppOverlaysContainer({
	currentBranchName,
	fileDiscovery,
	canCreateStash,
	onRefreshRepository,
	onRepositoryChanged,
	overlays,
	repositoryId,
	setCurrentBranchName,
	setDetailsPanel,
}: {
	currentBranchName: string | null;
	canCreateStash: boolean;
	fileDiscovery: ReturnType<typeof useFileDiscoveryTargets>;
	onRefreshRepository: () => void | Promise<void>;
	onRepositoryChanged: () => void;
	overlays: ReturnType<typeof useAppOverlayState>;
	repositoryId: string | null;
	setCurrentBranchName: Dispatch<SetStateAction<string | null>>;
	setDetailsPanel: Dispatch<SetStateAction<DetailsPanelState | null>>;
}) {
	return (
		<AppOverlays
			canCreateStash={canCreateStash}
			createBranchOpen={overlays.createBranchOpen}
			currentBranchName={currentBranchName}
			fileBlameTarget={fileDiscovery.blameTarget}
			fileHistoryTarget={fileDiscovery.historyTarget}
			isCommandPaletteOpen={overlays.commandPaletteOpen}
			isCommitSearchOpen={overlays.commitSearchOpen}
			onBranchChanged={(branchName) => {
				setCurrentBranchName(branchName);
				onRepositoryChanged();
			}}
			onCommandPaletteOpenChange={overlays.setCommandPaletteOpen}
			onCreateBranchOpenChange={overlays.setCreateBranchOpen}
			onFileBlameOpenChange={(open) => !open && fileDiscovery.closeBlame()}
			onFileHistoryOpenChange={(open) => !open && fileDiscovery.closeHistory()}
			onOpenSettings={() => overlays.setSettingsOpen(true)}
			onOpenWorkingChanges={() => setDetailsPanel({ kind: "workingChanges" })}
			onRefreshRepository={onRefreshRepository}
			onRemoteManagerOpenChange={overlays.setRemoteManagerOpen}
			onRepositoryChanged={onRepositoryChanged}
			onSearchOpenChange={overlays.setCommitSearchOpen}
			onSettingsOpenChange={overlays.setSettingsOpen}
			onStashOpenChange={overlays.setStashOpen}
			onSelectCommit={(commitHash) =>
				setDetailsPanel({ commitHash, kind: "commit" })
			}
			remoteManagerOpen={overlays.remoteManagerOpen}
			repositoryId={repositoryId}
			settingsOpen={overlays.settingsOpen}
			stashOpen={overlays.stashOpen}
		/>
	);
}
