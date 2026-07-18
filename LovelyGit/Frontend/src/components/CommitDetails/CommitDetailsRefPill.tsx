import { Cloud, GitBranch, Tag } from "@/components/icons/lovelyIcons";
import type { RepositoryRefItem } from "@/generated/types";
import { cn } from "@/lib/utils";

export function CommitDetailsRefPill({
	refItem,
	wide = false,
}: {
	refItem: RepositoryRefItem;
	wide?: boolean;
}) {
	const Icon =
		refItem.kind === "Tag"
			? Tag
			: refItem.kind === "Remote"
				? Cloud
				: GitBranch;
	return (
		<span
			className={cn(
				"inline-flex max-w-full items-center gap-1 rounded border bg-card px-1.5 py-0.5 text-xs text-muted-foreground",
				wide && "w-full",
			)}
			title={refItem.name}
		>
			<Icon aria-hidden="true" className="size-3 shrink-0" />
			<span className="truncate">{refItem.name}</span>
		</span>
	);
}
