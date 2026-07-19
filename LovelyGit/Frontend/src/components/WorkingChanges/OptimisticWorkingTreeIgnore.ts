import type { WorkingTreeChangesResponse } from "@/generated/types";

export function applyOptimisticIgnore(
	changes: WorkingTreeChangesResponse,
	path: string,
): WorkingTreeChangesResponse {
	const untracked = removePath(changes.untracked, path);
	if (untracked === changes.untracked) return changes;

	return {
		...changes,
		untracked,
		totalCount:
			changes.totalCount - (changes.untracked.length - untracked.length),
	};
}

function removePath(
	files: WorkingTreeChangesResponse["untracked"],
	path: string,
) {
	const index = lowerBound(files, path);
	if (files[index]?.path !== path) return files;

	let end = index + 1;
	while (files[end]?.path === path) end++;
	const retained = new Array<(typeof files)[number]>(
		files.length - (end - index),
	);
	let writeIndex = 0;
	for (let readIndex = 0; readIndex < files.length; readIndex++) {
		if (readIndex < index || readIndex >= end) {
			retained[writeIndex++] = files[readIndex];
		}
	}
	return retained;
}

function lowerBound(
	files: WorkingTreeChangesResponse["untracked"],
	path: string,
) {
	let low = 0;
	let high = files.length;
	while (low < high) {
		const middle = (low + high) >>> 1;
		if ((files[middle]?.path ?? "") < path) low = middle + 1;
		else high = middle;
	}
	return low;
}
