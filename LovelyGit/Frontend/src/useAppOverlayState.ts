import { useState } from "react";
import { useAppShortcuts } from "./useAppShortcuts";

export function useAppOverlayState(hasRepository: boolean) {
	const [commitSearchOpen, setCommitSearchOpen] = useState(false);
	const [commandPaletteOpen, setCommandPaletteOpen] = useState(false);
	const [settingsOpen, setSettingsOpen] = useState(false);
	const [createBranchOpen, setCreateBranchOpen] = useState(false);
	const [remoteManagerOpen, setRemoteManagerOpen] = useState(false);
	useAppShortcuts({
		hasRepository,
		onOpenCommandPalette: () => setCommandPaletteOpen(true),
		onOpenCommitSearch: () => setCommitSearchOpen(true),
	});
	return {
		commandPaletteOpen,
		commitSearchOpen,
		createBranchOpen,
		remoteManagerOpen,
		setCommandPaletteOpen,
		setCommitSearchOpen,
		setCreateBranchOpen,
		setRemoteManagerOpen,
		setSettingsOpen,
		settingsOpen,
	};
}
