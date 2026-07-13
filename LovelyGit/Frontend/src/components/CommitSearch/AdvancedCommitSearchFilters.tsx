import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { GitBranch, UserRound, X } from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import type { CommitSearchFilters as Filters } from "./commitSearchFilters";

export function AdvancedCommitSearchFilters({
	filters,
	onChange,
	onClear,
	open,
}: {
	filters: Filters;
	onChange: (filters: Filters) => void;
	onClear: () => void;
	open: boolean;
}) {
	const reduceMotion = useReducedMotion();
	const updateAuthor = (value: string) =>
		onChange({ ...filters, author: value });
	const updateScope = (value: string) => onChange({ ...filters, scope: value });
	return (
		<AnimatePresence initial={false}>
			{open ? (
				<motion.div
					animate={{ height: "auto", opacity: 1 }}
					className="overflow-hidden border-b bg-muted/20"
					exit={{ height: 0, opacity: 0 }}
					initial={{ height: 0, opacity: 0 }}
					transition={{ duration: reduceMotion ? 0 : 0.16 }}
				>
					<div className="grid grid-cols-[1fr_1fr_auto] items-end gap-2 p-3">
						<label
							className="grid gap-1 text-muted-foreground text-xs"
							htmlFor="commit-search-author"
						>
							<span className="flex items-center gap-1">
								<UserRound aria-hidden="true" className="size-3" /> Author
							</span>
							<Input
								aria-label="Filter by author"
								className="h-8"
								id="commit-search-author"
								onChange={(event) => updateAuthor(event.currentTarget.value)}
								onInput={(event) => updateAuthor(event.currentTarget.value)}
								placeholder="Name or email"
								value={filters.author}
							/>
						</label>
						<label
							className="grid gap-1 text-muted-foreground text-xs"
							htmlFor="commit-search-scope"
						>
							<span className="flex items-center gap-1">
								<GitBranch aria-hidden="true" className="size-3" /> Branch or
								tag
							</span>
							<Input
								aria-label="Limit search to branch or tag"
								className="h-8"
								id="commit-search-scope"
								onChange={(event) => updateScope(event.currentTarget.value)}
								onInput={(event) => updateScope(event.currentTarget.value)}
								placeholder="main, origin/main, or v1.0"
								value={filters.scope}
							/>
						</label>
						<Button
							aria-label="Clear search filters"
							onClick={onClear}
							size="icon-sm"
							variant="ghost"
						>
							<X aria-hidden="true" />
						</Button>
						<DateFilter
							label="From"
							name="afterDate"
							{...{ filters, onChange }}
						/>
						<DateFilter
							label="Until"
							name="beforeDate"
							{...{ filters, onChange }}
						/>
						<div aria-hidden="true" />
					</div>
				</motion.div>
			) : null}
		</AnimatePresence>
	);
}

function DateFilter({
	filters,
	label,
	name,
	onChange,
}: {
	filters: Filters;
	label: string;
	name: "afterDate" | "beforeDate";
	onChange: (filters: Filters) => void;
}) {
	const update = (value: string) => onChange({ ...filters, [name]: value });
	return (
		<label
			className="grid gap-1 text-muted-foreground text-xs"
			htmlFor={`commit-search-${name}`}
		>
			<span>{label}</span>
			<Input
				aria-label={`${label} commit date`}
				className="h-8"
				id={`commit-search-${name}`}
				onChange={(event) => update(event.currentTarget.value)}
				onInput={(event) => update(event.currentTarget.value)}
				type="date"
				value={filters[name]}
			/>
		</label>
	);
}
