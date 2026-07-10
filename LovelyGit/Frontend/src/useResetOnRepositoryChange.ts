import { type Dispatch, type SetStateAction, useEffect, useRef } from "react";
import type { DetailsPanelState } from "./AppPanelState";

export function useResetOnRepositoryChange(
	repositoryId: string | null,
	setBranchName: Dispatch<SetStateAction<string | null>>,
	setDetailsPanel: Dispatch<SetStateAction<DetailsPanelState | null>>,
	setSearchOpen: Dispatch<SetStateAction<boolean>>,
	resetFileDiscovery: () => void,
) {
	const previousRepositoryIdRef = useRef<string | null>(repositoryId);
	useEffect(() => {
		if (previousRepositoryIdRef.current === repositoryId) return;
		previousRepositoryIdRef.current = repositoryId;
		setBranchName(null);
		setDetailsPanel(null);
		setSearchOpen(false);
		resetFileDiscovery();
	}, [
		repositoryId,
		setBranchName,
		setDetailsPanel,
		setSearchOpen,
		resetFileDiscovery,
	]);
}
