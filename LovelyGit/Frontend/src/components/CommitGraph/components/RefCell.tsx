import type { CommitGraphRow } from "@/generated/ExpressThat.LovelyGit.Services.Git.CommitGraph.Models";
import { refLabel } from "../utils/format";

export function RefCell({ row }: { row: CommitGraphRow }) {
	const refs = [...row.commit.branches, ...row.commit.tags];

	if (refs.length === 0) {
		return <div className="h-[17px]" />;
	}

	return (
		<div className="flex min-w-0 gap-1">
			{refs.slice(0, 2).map((ref) => (
				<span
					className="inline-flex h-[17px] max-w-24 items-center overflow-hidden whitespace-nowrap rounded-[3px] border border-border bg-secondary px-1 text-[11px] text-secondary-foreground"
					key={ref}
					title={ref}
				>
					{refLabel(ref)}
					<span className="ml-1 inline-block h-[7px] w-[7px] rotate-45 bg-destructive" />
				</span>
			))}
		</div>
	);
}
