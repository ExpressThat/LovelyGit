import { AlertTriangle, Search } from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import type { CommitSearchResponse } from "@/generated/types";
import { motion } from "@/lib/motion";
import { CommitSearchResultRow } from "./CommitSearchResultRow";

export type CommitSearchContentProps = {
	canSearchDeeper: boolean;
	error: string | null;
	isLoading: boolean;
	minimumQueryLength: number;
	onSearchDeeper: () => void;
	onSelect: (index: number) => void;
	onSelectIndex: (index: number) => void;
	query: string;
	reduceMotion: boolean;
	response: CommitSearchResponse | null;
	selectedIndex: number;
};

export function CommitSearchContent(props: CommitSearchContentProps) {
	if (props.error) {
		return (
			<p className="flex gap-2 rounded-lg border border-destructive/30 bg-destructive/8 p-3 text-destructive">
				<AlertTriangle aria-hidden="true" className="size-4 shrink-0" />
				{props.error}
			</p>
		);
	}
	if (!props.response) {
		return props.isLoading ? <SearchSkeleton /> : <SearchPrompt {...props} />;
	}
	if (props.response.results.length === 0) {
		return <EmptySearch {...props} response={props.response} />;
	}
	return (
		<div className="grid gap-1">
			{props.response.isPartial ? (
				<SearchLimitNotice {...props} response={props.response} />
			) : null}
			<motion.div
				animate={{ opacity: 1, y: 0 }}
				className="grid gap-1"
				initial={{ opacity: 0, y: props.reduceMotion ? 0 : 4 }}
			>
				{props.response.results.map((result, index) => (
					<CommitSearchResultRow
						index={index}
						isSelected={props.selectedIndex === index}
						key={result.hash}
						onSelect={() => props.onSelect(index)}
						onSelectIndex={() => props.onSelectIndex(index)}
						query={props.query}
						result={result}
					/>
				))}
			</motion.div>
		</div>
	);
}

function SearchPrompt({ minimumQueryLength, query }: CommitSearchContentProps) {
	return (
		<div className="grid place-items-center gap-2 py-16 text-center text-muted-foreground">
			<Search aria-hidden="true" className="size-8 text-primary/60" />
			<p>
				{query.trim()
					? `Enter at least ${minimumQueryLength} characters.`
					: "Find any reachable commit."}
			</p>
			<p className="text-xs">
				Results stay local and use LovelyGit's native parser.
			</p>
		</div>
	);
}

function EmptySearch(
	props: CommitSearchContentProps & { response: CommitSearchResponse },
) {
	if (props.response.isPartial) {
		return (
			<div className="grid place-items-center gap-3 py-16 text-center text-muted-foreground">
				<SearchLimitNotice {...props} />
			</div>
		);
	}
	return (
		<p className="py-16 text-center text-muted-foreground">
			No commits match “{props.query.trim()}”.
		</p>
	);
}

function SearchLimitNotice({
	canSearchDeeper,
	onSearchDeeper,
	response,
}: CommitSearchContentProps & { response: CommitSearchResponse }) {
	return (
		<div className="flex items-center justify-between gap-3 rounded-lg border bg-card px-3 py-2 text-muted-foreground text-xs">
			<span>
				{canSearchDeeper
					? `Searched the first ${response.scannedCommitCount.toLocaleString()} commits to stay responsive.`
					: `Deep search reached its ${response.scannedCommitCount.toLocaleString()}-commit safety limit.`}
			</span>
			{canSearchDeeper ? (
				<Button onClick={onSearchDeeper} size="xs" variant="secondary">
					<Search aria-hidden="true" /> Search deeper
				</Button>
			) : null}
		</div>
	);
}

function SearchSkeleton() {
	return (
		<div
			aria-label="Searching commit history"
			className="grid gap-1"
			role="status"
		>
			{[1, 2, 3, 4, 5].map((key) => (
				<div className="h-[68px] animate-pulse rounded-lg bg-card" key={key} />
			))}
		</div>
	);
}

export function CommitSearchStatus({
	response,
}: {
	response: CommitSearchResponse | null;
}) {
	if (!response) return <span aria-live="polite">Ready</span>;
	return (
		<span aria-live="polite">
			{response.results.length} of {response.matchingCommitCount} matches ·{" "}
			{response.scannedCommitCount.toLocaleString()} commits
			{response.isPartial ? " (bounded)" : ""}
		</span>
	);
}
