import type { FileBlameHunk } from "@/generated/types";
import { formatDate, shortHash } from "../CommitGraph/utils/format";

export function FileBlameRow({
	hunk,
	line,
	lineNumber,
	onSelectCommit,
}: {
	hunk: FileBlameHunk | null;
	line: string;
	lineNumber: number;
	onSelectCommit: (hash: string) => void;
}) {
	const isHunkStart = hunk?.startLine === lineNumber;
	const label = hunk?.hash
		? `Line ${lineNumber}, ${hunk.author ?? "unknown"}, ${shortHash(hunk.hash)}`
		: `Line ${lineNumber}, attribution pending`;
	return (
		<div className="grid h-6 min-w-max grid-cols-[250px_62px_minmax(500px,1fr)] border-b border-border/35 font-mono text-xs leading-6 hover:bg-accent/50">
			<button
				aria-label={label}
				className="group flex min-w-0 items-center gap-2 border-r bg-card/55 px-2 text-left hover:bg-accent disabled:cursor-default"
				disabled={!hunk?.hash}
				onClick={() => hunk?.hash && onSelectCommit(hunk.hash)}
				title={hunk?.subject ?? "Older history is still being resolved"}
				type="button"
			>
				<span className="h-full w-0.5 shrink-0 bg-primary/55 group-hover:bg-primary" />
				{isHunkStart ? (
					<>
						<span className="max-w-28 truncate font-sans font-medium text-foreground">
							{hunk.author ?? "Unknown"}
						</span>
						<span className="text-muted-foreground">
							{hunk.hash ? shortHash(hunk.hash) : "pending"}
						</span>
						{hunk.date ? (
							<span className="ml-auto truncate font-sans text-[10px] text-muted-foreground">
								{formatDate(hunk.date)}
							</span>
						) : null}
					</>
				) : null}
			</button>
			<span className="select-none border-r bg-muted/30 px-3 text-right text-muted-foreground">
				{lineNumber}
			</span>
			<code className="whitespace-pre px-3 text-foreground">{line || " "}</code>
		</div>
	);
}
