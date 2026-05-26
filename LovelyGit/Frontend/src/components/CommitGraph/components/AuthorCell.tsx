import type { CommitGraphRow } from "../types/graph";
import { formatDate } from "../utils/format";
import { SkeletonShimmer } from "./SkeletonShimmer";

export function AuthorCell({ row }: { row: CommitGraphRow | null }) {
	if (!row) {
		return <SkeletonShimmer className="inline-block h-2 w-24 rounded-full" />;
	}

	return (
		<div
			className="truncate text-muted-foreground"
			title={`${row.commit.author} <${row.commit.email || "unknown"}>`}
		>
			{row.commit.author}{" "}
			<span className="text-muted-foreground/75">
				{formatDate(row.commit.date)}
			</span>
		</div>
	);
}
