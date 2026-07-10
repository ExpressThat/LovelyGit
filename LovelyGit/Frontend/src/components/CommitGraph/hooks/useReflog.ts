import { useEffect, useMemo, useState } from "react";
import type { GitReflogEntry } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";

export function useReflog(
	repositoryId: string | null,
	branchName: string | null,
) {
	const [entries, setEntries] = useState<GitReflogEntry[]>([]);
	const [query, setQuery] = useState("");
	const [error, setError] = useState<string | null>(null);
	const [isLoading, setIsLoading] = useState(true);

	useEffect(() => {
		if (!repositoryId || !branchName) return;
		let active = true;
		setIsLoading(true);
		setError(null);
		void sendRequestWithResponse({
			arguments: {
				branchName,
				knownRepositoryId: repositoryId,
				limit: 200,
			},
			commandType: "GetReflog",
		})
			.then((response) => {
				if (active) setEntries(response.entries);
			})
			.catch((reason) => {
				if (active) {
					setEntries([]);
					setError(
						reason instanceof Error ? reason.message : "Failed to read reflog.",
					);
				}
			})
			.finally(() => {
				if (active) setIsLoading(false);
			});
		return () => {
			active = false;
		};
	}, [branchName, repositoryId]);

	const filteredEntries = useMemo(
		() => filterReflogEntries(entries, query),
		[entries, query],
	);
	return { entries, error, filteredEntries, isLoading, query, setQuery };
}

export function filterReflogEntries(entries: GitReflogEntry[], query: string) {
	const normalized = query.trim().toLocaleLowerCase();
	if (!normalized) return entries;
	return entries.filter((entry) =>
		`${entry.selector} ${entry.oldHash} ${entry.newHash} ${entry.actorName} ${entry.actorEmail} ${entry.message}`
			.toLocaleLowerCase()
			.includes(normalized),
	);
}
