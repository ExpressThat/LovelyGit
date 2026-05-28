import type { CommitGraphRow } from "@/generated/ExpressThat.LovelyGit.Services.Git.CommitGraph.Models";
import { shortHash } from "../utils/format";

export function HashCell({ row }: { row: CommitGraphRow }) {
	return (
		<span className="font-mono text-muted-foreground">
			{shortHash(row.commit.hash)}
		</span>
	);
}
