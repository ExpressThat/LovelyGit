import type { CommitGraphRow } from "../types/graph";
import { shortHash } from "../utils/format";

export function HashCell({ row }: { row: CommitGraphRow }) {
	return (
		<span className="font-mono text-muted-foreground">
			{shortHash(row.commit.hash)}
		</span>
	);
}
