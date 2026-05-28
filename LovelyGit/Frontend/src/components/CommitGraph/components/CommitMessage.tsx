import type { CommitGraphRow } from "../types/graph";
import { messagePrefix } from "../utils/format";

export function CommitMessage({ row }: { row: CommitGraphRow }) {
	const prefix = messagePrefix(row);
	const details =
		row.isMergeCommit && row.commit.message && row.commit.message !== prefix
			? row.commit.message
			: "";

	return (
		<div
			className="flex min-w-0 overflow-hidden whitespace-nowrap"
			title={row.commit.message}
		>
			<span className="mr-2 shrink-0 font-semibold text-foreground">
				{prefix}
			</span>
			{details ? (
				<span className="min-w-0 flex-1 truncate text-muted-foreground">
					{details}
				</span>
			) : null}
		</div>
	);
}
