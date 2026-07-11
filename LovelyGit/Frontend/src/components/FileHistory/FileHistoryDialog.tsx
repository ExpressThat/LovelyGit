import { useEffect, useState } from "react";
import {
	CornerDownLeft,
	FileClock,
	LoaderCircle,
	Search,
} from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { FileHistoryResultRow } from "./FileHistoryResultRow";
import { useFileHistory } from "./useFileHistory";

export type FileHistoryTarget = {
	path: string;
	startCommitHash: string | null;
};

export function FileHistoryDialog({
	onOpenChange,
	onSelectCommit,
	repositoryId,
	target,
}: {
	onOpenChange: (open: boolean) => void;
	onSelectCommit: (commitHash: string) => void;
	repositoryId: string | null;
	target: FileHistoryTarget | null;
}) {
	const [deep, setDeep] = useState(false);
	const [query, setQuery] = useState("");
	const [selectedIndex, setSelectedIndex] = useState(0);
	const open = Boolean(target && repositoryId);
	const { error, isLoading, response } = useFileHistory(
		repositoryId,
		target?.path ?? null,
		target?.startCommitHash ?? null,
		open,
		deep,
	);
	const results = (response?.results ?? []).filter((result) => {
		const value = query.trim().toLocaleLowerCase();
		return (
			!value ||
			`${result.subject} ${result.author} ${result.hash}`
				.toLocaleLowerCase()
				.includes(value)
		);
	});
	const activeIndex = Math.min(selectedIndex, Math.max(results.length - 1, 0));

	useEffect(() => {
		if (!open) {
			setDeep(false);
			setQuery("");
			setSelectedIndex(0);
		}
	}, [open]);
	useEffect(() => {
		document
			.getElementById(`file-history-result-${activeIndex}`)
			?.scrollIntoView?.({ block: "nearest" });
	}, [activeIndex]);

	const selectResult = (index: number) => {
		const result = results[index];
		if (!result) return;
		onOpenChange(false);
		onSelectCommit(result.hash);
	};
	const updateQuery = (value: string) => {
		setQuery(value);
		setSelectedIndex(0);
	};

	return (
		<Dialog open={open} onOpenChange={onOpenChange}>
			<DialogContent className="gap-0 overflow-hidden p-0 sm:max-w-3xl">
				<DialogHeader className="gap-1 border-b px-4 py-3 pr-12">
					<DialogTitle className="flex items-center gap-2">
						<FileClock aria-hidden="true" className="size-5 text-primary" />{" "}
						File history
					</DialogTitle>
					<DialogDescription
						className="truncate font-mono"
						title={target?.path}
					>
						{target?.path}
					</DialogDescription>
				</DialogHeader>
				<div className="relative border-b p-3">
					<Search
						aria-hidden="true"
						className="absolute left-5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
					/>
					<Input
						aria-label="Filter file history"
						autoFocus
						className="h-10 pl-9 pr-10"
						onChange={(event) => updateQuery(event.currentTarget.value)}
						onInput={(event) => updateQuery(event.currentTarget.value)}
						onKeyDown={(event) => {
							if (event.key === "ArrowDown") {
								event.preventDefault();
								setSelectedIndex((value) =>
									Math.min(value + 1, results.length - 1),
								);
							} else if (event.key === "ArrowUp") {
								event.preventDefault();
								setSelectedIndex((value) => Math.max(value - 1, 0));
							} else if (event.key === "Enter") {
								event.preventDefault();
								selectResult(activeIndex);
							}
						}}
						placeholder="Filter by message, author, or hash"
						value={query}
					/>
					{isLoading ? (
						<LoaderCircle
							aria-label="Loading file history"
							className="absolute right-5 top-1/2 size-4 -translate-y-1/2 animate-spin text-primary"
						/>
					) : null}
				</div>
				<div className="custom-scrollbar h-[min(58vh,520px)] overflow-y-auto p-2">
					{error ? <Message tone="error">{error}</Message> : null}
					{!error && !isLoading && results.length === 0 ? (
						<Message>No history found for this path.</Message>
					) : null}
					{results.map((result, index) => (
						<FileHistoryResultRow
							key={result.hash}
							index={index}
							isSelected={index === activeIndex}
							onSelect={() => selectResult(index)}
							onSelectIndex={() => setSelectedIndex(index)}
							result={result}
						/>
					))}
					{response?.isPartial && !deep ? (
						<Button
							className="mt-2 w-full"
							onClick={() => setDeep(true)}
							variant="outline"
						>
							Search deeper history
						</Button>
					) : null}
				</div>
				<div className="flex items-center justify-between border-t bg-muted/30 px-4 py-2 text-[11px] text-muted-foreground">
					<span>
						{response
							? `${response.matchingCommitCount.toLocaleString()} changes across ${response.scannedCommitCount.toLocaleString()} commits`
							: "Native repository history"}
					</span>
					<span className="flex items-center gap-2">
						↑↓ Navigate{" "}
						<span className="flex items-center gap-1">
							<CornerDownLeft aria-hidden="true" className="size-3" /> Open
						</span>{" "}
						Esc Close
					</span>
				</div>
			</DialogContent>
		</Dialog>
	);
}

function Message({
	children,
	tone = "muted",
}: {
	children: string;
	tone?: "error" | "muted";
}) {
	return (
		<div
			className={
				tone === "error"
					? "m-2 rounded-md border border-destructive/40 bg-destructive/10 p-3 text-destructive"
					: "p-8 text-center text-muted-foreground"
			}
		>
			{children}
		</div>
	);
}
