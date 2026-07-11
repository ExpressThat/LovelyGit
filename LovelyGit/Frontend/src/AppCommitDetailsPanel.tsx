import * as LazySurfaces from "./AppLazySurfaces";
import {
	type CommitDetailsPanelState,
	commitDetailsPanel,
} from "./AppPanelState";
import type { CommitChangedFile } from "./generated/types";

export function AppCommitDetailsPanel({
	onOpenFileBlame,
	onOpenFileHistory,
	onPanelChange,
	panel,
	refreshToken,
	repositoryId,
}: {
	onOpenFileBlame: (file: CommitChangedFile) => void;
	onOpenFileHistory: (file: CommitChangedFile) => void;
	onPanelChange: (panel: CommitDetailsPanelState) => void;
	panel: CommitDetailsPanelState;
	refreshToken: number;
	repositoryId: string;
}) {
	return (
		<LazySurfaces.CommitDetailsSurface
			commitHash={panel.commitHash}
			onOpenFileBlame={onOpenFileBlame}
			onOpenFileHistory={onOpenFileHistory}
			onParentIndexChange={(parentIndex) =>
				onPanelChange(commitDetailsPanel(panel.commitHash, parentIndex))
			}
			onSelectFile={(file) =>
				onPanelChange(
					commitDetailsPanel(panel.commitHash, panel.parentIndex, file),
				)
			}
			parentIndex={panel.parentIndex ?? 0}
			refreshToken={refreshToken}
			repositoryId={repositoryId}
		/>
	);
}
