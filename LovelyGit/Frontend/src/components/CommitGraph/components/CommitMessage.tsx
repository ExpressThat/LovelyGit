import type { CommitGraphRow } from "../types/graph";
import { messagePrefix } from "../utils/format";
import { SkeletonShimmer } from "./SkeletonShimmer";

export function CommitMessage({ row }: { row: CommitGraphRow | null }) {
	if (!row) {
		return (
			<SkeletonShimmer className="inline-block h-2 w-[220px] rounded-full" />
		);
	}

	const prefix = messagePrefix(row);
	const details =
		row.is_merge_commit && row.commit.message && row.commit.message !== prefix
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
