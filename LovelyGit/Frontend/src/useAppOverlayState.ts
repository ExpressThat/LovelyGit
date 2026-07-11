import { useState } from "react";
import { useAppShortcuts } from "./useAppShortcuts";

export function useAppOverlayState(hasRepository: boolean) {
	const [commitSearchOpen, setCommitSearchOpen] = useState(false);
	const [commandPaletteOpen, setCommandPaletteOpen] = useState(false);
	const [settingsOpen, setSettingsOpen] = useState(false);
	useAppShortcuts({
		hasRepository,
		onOpenCommandPalette: () => setCommandPaletteOpen(true),
		onOpenCommitSearch: () => setCommitSearchOpen(true),
	});
	return {
		commandPaletteOpen,
		commitSearchOpen,
		setCommandPaletteOpen,
		setCommitSearchOpen,
		setSettingsOpen,
		settingsOpen,
	};
}
