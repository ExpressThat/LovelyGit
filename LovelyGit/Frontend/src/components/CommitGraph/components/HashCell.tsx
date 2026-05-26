import type { CommitGraphRow } from "../types/graph";
import { shortHash } from "../utils/format";
import { SkeletonShimmer } from "./SkeletonShimmer";

export function HashCell({ row }: { row: CommitGraphRow | null }) {
	if (!row) {
		return <SkeletonShimmer className="inline-block h-2 w-14 rounded-full" />;
	}

	return (
		<span className="font-mono text-muted-foreground">
			{shortHash(row.commit.hash)}
		</span>
	);
}
