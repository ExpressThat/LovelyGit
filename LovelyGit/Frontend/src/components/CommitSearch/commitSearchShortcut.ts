export function isCommitSearchShortcut(event: KeyboardEvent) {
	return (
		(event.ctrlKey || event.metaKey) &&
		!event.altKey &&
		event.key.toLocaleLowerCase() === "f"
	);
}
