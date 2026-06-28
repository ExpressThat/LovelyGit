import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";

export function applyObservedWorkingTreeChanges(
	current: WorkingTreeChangesResponse | null,
	observedChanges: WorkingTreeChangedFile[] | null | undefined,
): WorkingTreeChangesResponse | null {
	if (!observedChanges || observedChanges.length === 0) {
		return current;
	}

	const next = cloneChanges(current);
	for (const change of observedChanges) {
		if (
			change.status === "Deleted" &&
			removeUntrackedAddition(next, change.path)
		) {
			continue;
		}

		removePath(next, change.path);
		targetGroup(next, change).push(change);
	}

	sortGroup(next.staged);
	sortGroup(next.unstaged);
	sortGroup(next.untracked);
	sortGroup(next.unmerged);
	return withTotalCount(next);
}

export function createEmptyWorkingTreeChanges(): WorkingTreeChangesResponse {
	return {
		staged: [],
		unstaged: [],
		untracked: [],
		unmerged: [],
		totalCount: 0,
	};
}

export function countObservedNewPaths(
	current: WorkingTreeChangesResponse | null,
	observedChanges: WorkingTreeChangedFile[] | null | undefined,
) {
	if (!observedChanges || observedChanges.length === 0) {
		return 0;
	}

	const existing = new Set(allFiles(current).map((file) => file.path));
	return observedChanges.filter((file) => !existing.has(file.path)).length;
}

function cloneChanges(
	current: WorkingTreeChangesResponse | null,
): WorkingTreeChangesResponse {
	return {
		staged: [...(current?.staged ?? [])],
		unstaged: [...(current?.unstaged ?? [])],
		untracked: [...(current?.untracked ?? [])],
		unmerged: [...(current?.unmerged ?? [])],
		totalCount: current?.totalCount ?? 0,
	};
}

function removePath(changes: WorkingTreeChangesResponse, path: string) {
	changes.staged = changes.staged.filter((file) => file.path !== path);
	changes.unstaged = changes.unstaged.filter((file) => file.path !== path);
	changes.untracked = changes.untracked.filter((file) => file.path !== path);
	changes.unmerged = changes.unmerged.filter((file) => file.path !== path);
}

function removeUntrackedAddition(
	changes: WorkingTreeChangesResponse,
	path: string,
) {
	const previousLength = changes.untracked.length;
	changes.untracked = changes.untracked.filter(
		(file) => file.path !== path || file.status !== "Added",
	);
	return changes.untracked.length !== previousLength;
}

function targetGroup(
	changes: WorkingTreeChangesResponse,
	file: WorkingTreeChangedFile,
) {
	switch (file.group) {
		case "Staged":
			return changes.staged;
		case "Untracked":
			return changes.untracked;
		case "Unmerged":
			return changes.unmerged;
		default:
			return changes.unstaged;
	}
}

function withTotalCount(
	changes: WorkingTreeChangesResponse,
): WorkingTreeChangesResponse {
	return {
		...changes,
		totalCount: allFiles(changes).length,
	};
}

function allFiles(changes: WorkingTreeChangesResponse | null) {
	return [
		...(changes?.staged ?? []),
		...(changes?.unstaged ?? []),
		...(changes?.untracked ?? []),
		...(changes?.unmerged ?? []),
	];
}

function sortGroup(files: WorkingTreeChangedFile[]) {
	files.sort((left, right) => left.path.localeCompare(right.path));
}
