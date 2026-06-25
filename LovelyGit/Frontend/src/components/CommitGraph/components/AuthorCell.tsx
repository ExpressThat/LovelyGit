import type { CommitGraphRow } from "@/generated/types";
import { formatDate } from "../utils/format";

export function AuthorCell({ row }: { row: CommitGraphRow }) {
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
