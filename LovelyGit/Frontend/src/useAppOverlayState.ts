import { useCallback, useState } from "react";
import { useAppShortcuts } from "./useAppShortcuts";

export function useAppOverlayState(hasRepository: boolean) {
	const [commitSearchOpen, setCommitSearchOpen] = useState(false);
	const [commandPaletteOpen, setCommandPaletteOpen] = useState(false);
	const [settingsOpen, setSettingsOpen] = useState(false);
	const [createBranchOpen, setCreateBranchOpen] = useState(false);
	const [remoteManagerOpen, setRemoteManagerOpen] = useState(false);
	const [stashOpen, setStashOpen] = useState(false);
	useAppShortcuts({
		hasRepository,
		onOpenCommandPalette: () => setCommandPaletteOpen(true),
		onOpenCommitSearch: () => setCommitSearchOpen(true),
	});
	const resetRepositoryOverlays = useCallback(() => {
		setCommandPaletteOpen(false);
		setCommitSearchOpen(false);
		setCreateBranchOpen(false);
		setRemoteManagerOpen(false);
		setStashOpen(false);
	}, []);
	return {
		commandPaletteOpen,
		commitSearchOpen,
		createBranchOpen,
		remoteManagerOpen,
		resetRepositoryOverlays,
		stashOpen,
		setCommandPaletteOpen,
		setCommitSearchOpen,
		setCreateBranchOpen,
		setRemoteManagerOpen,
		setStashOpen,
		setSettingsOpen,
		settingsOpen,
	};
}
