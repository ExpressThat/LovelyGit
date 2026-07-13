export type CommitSearchFilters = {
	author: string;
	afterDate: string;
	beforeDate: string;
	scope: string;
};

export const emptyCommitSearchFilters: CommitSearchFilters = {
	author: "",
	afterDate: "",
	beforeDate: "",
	scope: "",
};

export function toSearchBoundaries(filters: CommitSearchFilters) {
	return {
		afterUnixSeconds: dateStart(filters.afterDate),
		beforeUnixSeconds: filters.beforeDate
			? dateStart(filters.beforeDate, 1)
			: null,
	};
}

export function hasCommitSearchFilter(filters: CommitSearchFilters) {
	return Boolean(
		filters.author.trim() ||
			filters.scope.trim() ||
			filters.afterDate ||
			filters.beforeDate,
	);
}

export function isCommitSearchDateRangeValid(filters: CommitSearchFilters) {
	const { afterUnixSeconds, beforeUnixSeconds } = toSearchBoundaries(filters);
	return (
		afterUnixSeconds === null ||
		beforeUnixSeconds === null ||
		afterUnixSeconds < beforeUnixSeconds
	);
}

export function isKnownCommitSearchScope(
	scope: string,
	refs: RepositoryRefItem[],
) {
	const normalized = scope.trim();
	if (!normalized || normalized.toUpperCase() === "HEAD") return true;
	return refs.some((reference) => {
		if (reference.name === normalized) return true;
		const prefix =
			reference.kind === "Local"
				? "refs/heads/"
				: reference.kind === "Remote"
					? "refs/remotes/"
					: reference.kind === "Tag"
						? "refs/tags/"
						: "";
		return prefix.length > 0 && `${prefix}${reference.name}` === normalized;
	});
}

function dateStart(value: string, addDays = 0) {
	if (!value) return null;
	const [year, month, day] = value.split("-").map(Number);
	if (!year || !month || !day) return null;
	return Math.floor(Date.UTC(year, month - 1, day + addDays) / 1000);
}

import type { RepositoryRefItem } from "@/generated/types";
