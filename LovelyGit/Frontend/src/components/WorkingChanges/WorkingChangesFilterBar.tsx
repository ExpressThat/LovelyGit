import { Search, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import type { WorkingChangesFilterGroup } from "./WorkingChangesFilterUtils";

const groupOptions: WorkingChangesFilterGroup[] = [
	"All",
	"Staged",
	"Changes",
	"Unmerged",
];

export function WorkingChangesFilterBar({
	group,
	matchedCount,
	onGroupChange,
	onQueryChange,
	query,
	totalCount,
}: {
	group: WorkingChangesFilterGroup;
	matchedCount: number;
	onGroupChange: (group: WorkingChangesFilterGroup) => void;
	onQueryChange: (query: string) => void;
	query: string;
	totalCount: number;
}) {
	const isFiltered = query.trim().length > 0 || group !== "All";

	return (
		<div className="space-y-2">
			<div className="relative">
				<Search
					aria-hidden="true"
					className="-translate-y-1/2 pointer-events-none absolute top-1/2 left-2.5 size-4 text-muted-foreground"
				/>
				<Input
					aria-label="Filter working changes"
					className="pr-8 pl-8"
					onChange={(event) => onQueryChange(event.currentTarget.value)}
					onInput={(event) => onQueryChange(event.currentTarget.value)}
					placeholder="Filter files"
					value={query}
				/>
				{query.length > 0 ? (
					<Button
						aria-label="Clear working changes filter"
						className="-translate-y-1/2 absolute top-1/2 right-1 size-6"
						onClick={() => onQueryChange("")}
						size="icon-xs"
						title="Clear filter"
						type="button"
						variant="ghost"
					>
						<X aria-hidden="true" />
					</Button>
				) : null}
			</div>
			<div className="flex items-center justify-between gap-2">
				<div className="flex min-w-0 flex-wrap gap-1">
					{groupOptions.map((option) => (
						<Button
							aria-pressed={group === option}
							className="h-7 px-2 text-xs"
							key={option}
							onClick={() => onGroupChange(option)}
							type="button"
							variant={group === option ? "secondary" : "ghost"}
						>
							{option}
						</Button>
					))}
				</div>
				<div
					aria-live="polite"
					className="shrink-0 text-xs text-muted-foreground"
				>
					{isFiltered ? `${matchedCount}/${totalCount}` : `${totalCount}`}
				</div>
			</div>
		</div>
	);
}
