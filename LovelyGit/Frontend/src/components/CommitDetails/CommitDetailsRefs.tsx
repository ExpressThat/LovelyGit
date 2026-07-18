import { useMemo, useState } from "react";
import { ChevronDown, Search } from "@/components/icons/lovelyIcons";
import type { RepositoryRefItem } from "@/generated/types";
import { AnimatePresence, motion, useReducedMotion } from "@/lib/motion";
import { CommitDetailsRefList } from "./CommitDetailsRefList";
import { CommitDetailsRefPill } from "./CommitDetailsRefPill";

const PREVIEW_COUNT = 3;
const EXPAND_THRESHOLD = 6;

export function CommitDetailsRefs({ refs }: { refs: RepositoryRefItem[] }) {
	const [expanded, setExpanded] = useState(false);
	const [query, setQuery] = useState("");
	const reduceMotion = useReducedMotion();
	const filteredRefs = useMemo(() => {
		const normalized = query.trim().toLowerCase();
		return normalized
			? refs.filter((ref) => ref.name.toLowerCase().includes(normalized))
			: refs;
	}, [query, refs]);
	if (refs.length === 0) return null;
	const isLarge = refs.length > EXPAND_THRESHOLD;
	const visibleRefs = isLarge ? refs.slice(0, PREVIEW_COUNT) : refs;

	return (
		<section className="grid gap-2" data-commit-details-refs>
			<div className="flex items-center justify-between gap-2">
				<span className="font-medium text-xs">References</span>
				<span className="text-muted-foreground text-xs">
					{refs.length.toLocaleString()}
				</span>
			</div>
			<div className="flex flex-wrap gap-1.5">
				{visibleRefs.map((ref) => (
					<CommitDetailsRefPill key={`${ref.kind}:${ref.name}`} refItem={ref} />
				))}
				{isLarge ? (
					<button
						aria-expanded={expanded}
						className="inline-flex items-center gap-1 rounded border bg-muted px-1.5 py-0.5 text-muted-foreground text-xs hover:bg-accent hover:text-accent-foreground"
						onClick={() => setExpanded((current) => !current)}
						type="button"
					>
						{expanded
							? "Hide references"
							: `+${refs.length - PREVIEW_COUNT} more`}
						<ChevronDown
							aria-hidden="true"
							className={`size-3 transition-transform ${expanded ? "rotate-180" : ""}`}
						/>
					</button>
				) : null}
			</div>
			<AnimatePresence initial={false}>
				{expanded ? (
					<motion.div
						animate={{ opacity: 1, y: 0 }}
						className="grid gap-2"
						exit={{ opacity: 0, y: reduceMotion ? 0 : -4 }}
						initial={{ opacity: 0, y: reduceMotion ? 0 : -4 }}
					>
						<label className="relative">
							<span className="sr-only">Filter commit references</span>
							<Search
								aria-hidden="true"
								className="pointer-events-none absolute left-2 top-1/2 size-3.5 -translate-y-1/2 text-muted-foreground"
							/>
							<input
								className="h-7 w-full rounded-md border border-input bg-background pr-2 pl-7 text-xs outline-none focus-visible:border-ring focus-visible:ring-2 focus-visible:ring-ring/50"
								onInput={(event) => setQuery(event.currentTarget.value)}
								placeholder="Filter references"
								value={query}
							/>
						</label>
						{filteredRefs.length > 0 ? (
							<CommitDetailsRefList refs={filteredRefs} />
						) : (
							<p className="rounded-md border border-dashed p-3 text-center text-muted-foreground text-xs">
								No references match this filter.
							</p>
						)}
					</motion.div>
				) : null}
			</AnimatePresence>
		</section>
	);
}
