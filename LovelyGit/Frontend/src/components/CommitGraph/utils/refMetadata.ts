import type { CommitRefKind, RepositoryRefsResponse } from "@/generated/types";

export function refNames(
	response: RepositoryRefsResponse | null,
	kind: CommitRefKind,
) {
	return (response?.refs ?? [])
		.filter((reference) => reference.kind === kind)
		.map((reference) => reference.name);
}
