export function isCommandPaletteShortcut(event: KeyboardEvent) {
	return (
		event.key.toLocaleLowerCase() === "k" &&
		(event.ctrlKey || event.metaKey) &&
		!event.altKey &&
		!event.shiftKey
	);
}
