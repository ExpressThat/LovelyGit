import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";

const maxOptimisticObservedChanges = 25;

export function applyObservedWorkingTreeChanges(
	current: WorkingTreeChangesResponse | null,
	observedChanges: WorkingTreeChangedFile[] | null | undefined,
): WorkingTreeChangesResponse | null {
	if (!observedChanges || observedChanges.length === 0) {
		return current;
	}

	const paths = new Set(observedChanges.map((change) => change.path));
	const { latest, next } = partitionObservedPaths(current, paths);
	for (const change of observedChanges) {
		if (change.status === "Deleted") {
			const existing = latest.get(change.path) ?? [];
			const withoutUntrackedAddition = existing.filter(
				(file) => file.group !== "Untracked" || file.status !== "Added",
			);
			if (withoutUntrackedAddition.length !== existing.length) {
				latest.set(change.path, withoutUntrackedAddition);
				continue;
			}
		}

		latest.set(change.path, [change]);
	}
	const changedGroups = new Set<WorkingTreeChangedFile[]>();
	for (const changes of latest.values()) {
		for (const change of changes) {
			const group = targetGroup(next, change);
			group.push(change);
			changedGroups.add(group);
		}
	}

	for (const group of changedGroups) sortGroup(group);
	return withTotalCount(next);
}

function partitionObservedPaths(
	current: WorkingTreeChangesResponse | null,
	paths: Set<string>,
) {
	const latest = new Map<string, WorkingTreeChangedFile[]>();
	const retainUnobserved = (file: WorkingTreeChangedFile) => {
		if (!paths.has(file.path)) return true;
		const files = latest.get(file.path);
		if (files) files.push(file);
		else latest.set(file.path, [file]);
		return false;
	};
	return {
		latest,
		next: {
			isComplete: current?.isComplete ?? true,
			staged: (current?.staged ?? []).filter(retainUnobserved),
			unstaged: (current?.unstaged ?? []).filter(retainUnobserved),
			untracked: (current?.untracked ?? []).filter(retainUnobserved),
			unmerged: (current?.unmerged ?? []).filter(retainUnobserved),
			totalCount: current?.totalCount ?? 0,
		},
	};
}

export function shouldApplyObservedWorkingTreeChanges(
	observedChanges: WorkingTreeChangedFile[] | null | undefined,
) {
	return Boolean(
		observedChanges &&
			observedChanges.length > 0 &&
			observedChanges.length <= maxOptimisticObservedChanges,
	);
}

export function createEmptyWorkingTreeChanges(): WorkingTreeChangesResponse {
	return {
		isComplete: true,
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

	const existing = new Set<string>();
	if (current) {
		for (const file of current.staged) existing.add(file.path);
		for (const file of current.unstaged) existing.add(file.path);
		for (const file of current.untracked) existing.add(file.path);
		for (const file of current.unmerged) existing.add(file.path);
	}
	return observedChanges.filter((file) => !existing.has(file.path)).length;
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
		totalCount:
			changes.staged.length +
			changes.unstaged.length +
			changes.untracked.length +
			changes.unmerged.length,
	};
}

function sortGroup(files: WorkingTreeChangedFile[]) {
	files.sort((left, right) => left.path.localeCompare(right.path));
}
