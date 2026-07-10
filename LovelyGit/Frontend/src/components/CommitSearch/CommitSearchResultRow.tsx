import { GitCommitHorizontal } from "lucide-react";
import { motion } from "motion/react";
import { formatDate, shortHash } from "@/components/CommitGraph/utils/format";
import type { CommitSearchResult } from "@/generated/types";

export function CommitSearchResultRow({
	index,
	isSelected,
	onSelect,
	onSelectIndex,
	query,
	result,
}: {
	index: number;
	isSelected: boolean;
	onSelect: () => void;
	onSelectIndex: () => void;
	query: string;
	result: CommitSearchResult;
}) {
	const showPreview = result.preview !== result.subject;
	return (
		<button
			aria-label={`Open commit ${shortHash(result.hash)}: ${result.subject}`}
			className="group relative grid w-full grid-cols-[auto_minmax(0,1fr)_auto] gap-3 overflow-hidden rounded-lg border border-transparent px-3 py-2.5 text-left outline-none hover:border-border focus-visible:ring-2 focus-visible:ring-ring"
			id={`commit-search-result-${index}`}
			onClick={onSelect}
			onFocus={onSelectIndex}
			onMouseEnter={onSelectIndex}
			type="button"
		>
			{isSelected ? (
				<motion.span
					className="absolute inset-0 bg-accent"
					layoutId="commit-search-selection"
					transition={{ type: "spring", stiffness: 470, damping: 38 }}
				/>
			) : null}
			<span className="relative mt-0.5 rounded-md bg-primary/12 p-1.5 text-primary">
				<GitCommitHorizontal aria-hidden="true" className="size-4" />
			</span>
			<span className="relative min-w-0">
				<span className="flex min-w-0 items-center gap-1.5">
					<strong className="truncate text-sm">
						<HighlightedText query={query} text={result.subject} />
					</strong>
					{result.refs.map((reference) => (
						<span
							className="max-w-28 shrink-0 truncate rounded bg-secondary px-1.5 py-0.5 text-[10px] text-secondary-foreground"
							key={reference}
						>
							{reference}
						</span>
					))}
				</span>
				{showPreview ? (
					<span className="mt-0.5 block truncate text-muted-foreground text-xs">
						<HighlightedText query={query} text={result.preview} />
					</span>
				) : null}
				<span className="mt-1 block truncate text-muted-foreground text-[11px]">
					{result.author}
					{result.email ? ` <${result.email}>` : ""} · {formatDate(result.date)}
				</span>
			</span>
			<span className="relative pt-0.5 font-mono text-muted-foreground text-xs">
				{shortHash(result.hash)}
			</span>
		</button>
	);
}

export function HighlightedText({
	query,
	text,
}: {
	query: string;
	text: string;
}) {
	const index = text
		.toLocaleLowerCase()
		.indexOf(query.trim().toLocaleLowerCase());
	if (index < 0 || !query.trim()) return text;
	const end = index + query.trim().length;
	return (
		<>
			{text.slice(0, index)}
			<mark className="rounded-sm bg-primary/20 px-0.5 text-foreground">
				{text.slice(index, end)}
			</mark>
			{text.slice(end)}
		</>
	);
}
