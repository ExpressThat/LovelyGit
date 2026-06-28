export type RepositoryTabMenuAction =
	| "copy-path"
	| "reveal"
	| "open-terminal"
	| "select"
	| "close";

export function getRepositoryTabMenuActions({
	hasPath,
	isActive,
}: {
	hasPath: boolean;
	isActive: boolean;
}): RepositoryTabMenuAction[] {
	return [
		...(hasPath ? (["copy-path", "reveal", "open-terminal"] as const) : []),
		...(isActive ? [] : (["select"] as const)),
		"close",
	];
}
