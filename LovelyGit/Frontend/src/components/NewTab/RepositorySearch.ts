import type { KnownGitRepository } from "@/generated/types";

export function filterRepositories(
	repositories: KnownGitRepository[],
	query: string,
) {
	const terms = query.trim().toLocaleLowerCase().split(/\s+/).filter(Boolean);
	if (terms.length === 0) {
		return repositories;
	}

	return repositories.filter((repository) => {
		const haystack = `${repository.name ?? ""} ${repository.path ?? ""}`
			.toLocaleLowerCase()
			.replaceAll("\\", "/");
		return terms.every((term) => haystack.includes(term.replaceAll("\\", "/")));
	});
}
