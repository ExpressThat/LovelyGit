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
	const sortedPaths = [...paths].sort();
	return {
		latest,
		next: {
			isComplete: current?.isComplete ?? true,
			staged: partitionGroup(current?.staged ?? [], sortedPaths, latest),
			unstaged: partitionGroup(current?.unstaged ?? [], sortedPaths, latest),
			untracked: partitionGroup(current?.untracked ?? [], sortedPaths, latest),
			unmerged: partitionGroup(current?.unmerged ?? [], sortedPaths, latest),
			totalCount: current?.totalCount ?? 0,
		},
	};
}

function partitionGroup(
	files: WorkingTreeChangedFile[],
	paths: string[],
	latest: Map<string, WorkingTreeChangedFile[]>,
) {
	const removed: number[] = [];
	for (const path of paths) {
		let index = lowerBound(files, path);
		while (files[index]?.path === path) {
			const existing = latest.get(path);
			if (existing) existing.push(files[index]);
			else latest.set(path, [files[index]]);
			removed.push(index++);
		}
	}
	if (removed.length === 0) return files;
	const retained = new Array<WorkingTreeChangedFile>(
		files.length - removed.length,
	);
	let removeIndex = 0;
	let writeIndex = 0;
	for (let readIndex = 0; readIndex < files.length; readIndex++) {
		if (readIndex === removed[removeIndex]) {
			removeIndex++;
		} else {
			retained[writeIndex++] = files[readIndex];
		}
	}
	return retained;
}

function lowerBound(files: WorkingTreeChangedFile[], path: string) {
	let low = 0;
	let high = files.length;
	while (low < high) {
		const middle = (low + high) >>> 1;
		if ((files[middle]?.path ?? "") < path) low = middle + 1;
		else high = middle;
	}
	return low;
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
