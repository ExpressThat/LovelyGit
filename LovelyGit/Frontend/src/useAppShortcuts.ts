import { useEffect } from "react";
import { isCommandPaletteShortcut } from "./components/CommandPalette/commandPaletteShortcut";
import { isCommitSearchShortcut } from "./components/CommitSearch/commitSearchShortcut";

export function useAppShortcuts({
	hasRepository,
	onOpenCommandPalette,
	onOpenCommitSearch,
}: {
	hasRepository: boolean;
	onOpenCommandPalette: () => void;
	onOpenCommitSearch: () => void;
}) {
	useEffect(() => {
		const handleShortcut = (event: KeyboardEvent) => {
			if (isCommandPaletteShortcut(event)) {
				event.preventDefault();
				onOpenCommandPalette();
			} else if (hasRepository && isCommitSearchShortcut(event)) {
				event.preventDefault();
				onOpenCommitSearch();
			}
		};
		window.addEventListener("keydown", handleShortcut);
		return () => window.removeEventListener("keydown", handleShortcut);
	}, [hasRepository, onOpenCommandPalette, onOpenCommitSearch]);
}
