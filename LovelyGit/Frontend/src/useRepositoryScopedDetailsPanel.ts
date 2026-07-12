import { type Dispatch, type SetStateAction, useState } from "react";
import type { DetailsPanelState } from "./AppPanelState";
import { useResetOnRepositoryChange } from "./useResetOnRepositoryChange";

export function useRepositoryScopedDetailsPanel(
	repositoryId: string | null,
	setBranchName: Dispatch<SetStateAction<string | null>>,
	resetRepositoryOverlays: () => void,
	resetFileDiscovery: () => void,
) {
	const [detailsPanel, setDetailsPanel] = useState<DetailsPanelState | null>(
		null,
	);
	const isRepositoryStateCurrent = useResetOnRepositoryChange(
		repositoryId,
		setBranchName,
		setDetailsPanel,
		resetRepositoryOverlays,
		resetFileDiscovery,
	);
	return [
		isRepositoryStateCurrent ? detailsPanel : null,
		setDetailsPanel,
	] as const;
}
