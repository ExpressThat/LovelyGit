import { useReducedMotion } from "motion/react";
import { useEffect, useState } from "react";
import {
	CornerDownLeft,
	LoaderCircle,
	Search,
} from "@/components/icons/lovelyIcons";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { CommitSearchContent, CommitSearchStatus } from "./CommitSearchContent";
import { useCommitSearch } from "./useCommitSearch";

export function CommitSearchDialog({
	onOpenChange,
	onSelectCommit,
	open,
	repositoryId,
}: {
	onOpenChange: (open: boolean) => void;
	onSelectCommit: (commitHash: string) => void;
	open: boolean;
	repositoryId: string | null;
}) {
	const [query, setQuery] = useState("");
	const [deep, setDeep] = useState(false);
	const [selectedIndex, setSelectedIndex] = useState(0);
	const { error, isLoading, minimumQueryLength, response } = useCommitSearch(
		repositoryId,
		query,
		open,
		deep,
	);
	const reduceMotion = useReducedMotion();
	const results = response?.results ?? [];
	const activeIndex = Math.min(selectedIndex, Math.max(results.length - 1, 0));

	useEffect(() => {
		if (!open) {
			setQuery("");
			setDeep(false);
		}
	}, [open]);
	useEffect(() => {
		document
			.getElementById(`commit-search-result-${activeIndex}`)
			?.scrollIntoView?.({ block: "nearest" });
	}, [activeIndex]);

	const selectResult = (index: number) => {
		const result = results[index];
		if (!result) return;
		onOpenChange(false);
		onSelectCommit(result.hash);
	};
	return (
		<Dialog open={open} onOpenChange={onOpenChange}>
			<DialogContent className="gap-0 overflow-hidden p-0 sm:max-w-3xl">
				<DialogHeader className="gap-1 border-b px-4 py-3 pr-12">
					<DialogTitle className="flex items-center gap-2">
						<Search aria-hidden="true" className="size-5 text-primary" />
						Search commits
						<kbd className="ml-auto rounded border bg-muted px-1.5 py-0.5 font-mono text-[10px] text-muted-foreground">
							Ctrl F
						</kbd>
					</DialogTitle>
					<DialogDescription>
						Search reachable history by message, author, email, or hash.
					</DialogDescription>
				</DialogHeader>
				<div className="relative border-b p-3">
					<Search
						aria-hidden="true"
						className="absolute left-5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
					/>
					<Input
						aria-label="Search commit history"
						autoFocus
						className="h-10 pl-9 pr-10 text-sm"
						onChange={(event) => {
							setQuery(event.currentTarget.value);
							setSelectedIndex(0);
							setDeep(false);
						}}
						onInput={(event) => {
							setQuery(event.currentTarget.value);
							setSelectedIndex(0);
							setDeep(false);
						}}
						onKeyDown={(event) => {
							if (event.key === "ArrowDown") {
								event.preventDefault();
								setSelectedIndex((index) =>
									results.length === 0
										? 0
										: Math.min(index + 1, results.length - 1),
								);
							} else if (event.key === "ArrowUp") {
								event.preventDefault();
								setSelectedIndex((index) => Math.max(index - 1, 0));
							} else if (event.key === "Enter") {
								event.preventDefault();
								selectResult(activeIndex);
							}
						}}
						placeholder="Try a message, contributor, or 7-character hash"
						value={query}
					/>
					{isLoading ? (
						<LoaderCircle
							aria-label="Searching commits"
							className="absolute right-5 top-1/2 size-4 -translate-y-1/2 animate-spin text-primary"
						/>
					) : null}
				</div>
				<div className="custom-scrollbar h-[min(58vh,520px)] overflow-y-auto p-2">
					<CommitSearchContent
						error={error}
						isLoading={isLoading}
						canSearchDeeper={!deep}
						minimumQueryLength={minimumQueryLength}
						onSelect={selectResult}
						onSelectIndex={setSelectedIndex}
						onSearchDeeper={() => setDeep(true)}
						query={query}
						reduceMotion={Boolean(reduceMotion)}
						response={response}
						selectedIndex={activeIndex}
					/>
				</div>
				<div className="flex items-center justify-between border-t bg-muted/30 px-4 py-2 text-muted-foreground text-[11px]">
					<CommitSearchStatus response={response} />
					<span className="flex items-center gap-2">
						↑↓ Navigate
						<span className="flex items-center gap-1">
							<CornerDownLeft aria-hidden="true" className="size-3" /> Open
						</span>
						Esc Close
					</span>
				</div>
			</DialogContent>
		</Dialog>
	);
}
