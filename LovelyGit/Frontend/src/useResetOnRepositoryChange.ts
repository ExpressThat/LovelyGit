import { type Dispatch, type SetStateAction, useEffect, useRef } from "react";
import type { DetailsPanelState } from "./AppPanelState";
import type { FileHistoryTarget } from "./components/FileHistory/FileHistoryDialog";

export function useResetOnRepositoryChange(
	repositoryId: string | null,
	setBranchName: Dispatch<SetStateAction<string | null>>,
	setDetailsPanel: Dispatch<SetStateAction<DetailsPanelState | null>>,
	setFileHistoryTarget: Dispatch<SetStateAction<FileHistoryTarget | null>>,
	setSearchOpen: Dispatch<SetStateAction<boolean>>,
) {
	const previousRepositoryIdRef = useRef<string | null>(repositoryId);
	useEffect(() => {
		if (previousRepositoryIdRef.current === repositoryId) return;
		previousRepositoryIdRef.current = repositoryId;
		setBranchName(null);
		setDetailsPanel(null);
		setFileHistoryTarget(null);
		setSearchOpen(false);
	}, [
		repositoryId,
		setBranchName,
		setDetailsPanel,
		setFileHistoryTarget,
		setSearchOpen,
	]);
}
