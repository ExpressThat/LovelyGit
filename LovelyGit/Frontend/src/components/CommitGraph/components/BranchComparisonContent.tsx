import { useVirtualizer } from "@tanstack/react-virtual";
import type { CSSProperties, ReactNode } from "react";
import { useRef } from "react";
import { FileDiff, GitCommitHorizontal } from "@/components/icons/lovelyIcons";
import type {
	BranchComparisonCommit,
	BranchComparisonFile,
	BranchComparisonResponse,
} from "@/generated/types";
import { motion, useReducedMotion } from "@/lib/motion";
import { shortHash } from "../utils/format";

export type BranchComparisonSection = "ahead" | "behind" | "files";

const FILE_ROW_HEIGHT = 34;
const INITIAL_FILE_WINDOW = 12;

export function BranchComparisonContent({
	comparison,
	onOpenFile,
	section,
}: {
	comparison: BranchComparisonResponse;
	onOpenFile?: (file: BranchComparisonFile) => void;
	section: BranchComparisonSection;
}) {
	const reduceMotion = useReducedMotion();
	const transition = reduceMotion
		? { duration: 0 }
		: { duration: 0.16, ease: "easeOut" as const };
	return (
		<motion.div
			animate={{ opacity: 1, x: 0 }}
			className={
				section === "files"
					? "flex min-h-0 flex-1 flex-col overflow-hidden"
					: "custom-scrollbar min-h-0 flex-1 overflow-y-auto"
			}
			exit={{ opacity: 0, x: section === "behind" ? 8 : -8 }}
			initial={{ opacity: 0, x: section === "behind" ? -8 : 8 }}
			key={section}
			transition={transition}
		>
			{section === "files" ? (
				<FileList
					files={comparison.files}
					onOpenFile={onOpenFile}
					total={comparison.changedFileCount}
					truncated={comparison.isFileListTruncated}
				/>
			) : (
				<CommitList
					commits={
						section === "ahead"
							? comparison.aheadCommits
							: comparison.behindCommits
					}
					emptyMessage={
						section === "ahead"
							? `${comparison.currentBranchName} has no unique commits.`
							: `${comparison.targetBranchName} has no unique commits.`
					}
				/>
			)}
		</motion.div>
	);
}

function CommitList({
	commits,
	emptyMessage,
}: {
	commits: BranchComparisonCommit[];
	emptyMessage: string;
}) {
	if (commits.length === 0)
		return <EmptyState icon={<GitCommitHorizontal />} message={emptyMessage} />;
	return (
		<ul className="grid gap-1.5 pr-1">
			{commits.map((commit) => (
				<li
					className="grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-2 rounded-lg border bg-card px-2.5 py-2"
					key={commit.hash}
				>
					<GitCommitHorizontal className="size-4 text-primary" />
					<div className="min-w-0">
						<div className="truncate font-medium text-xs">{commit.subject}</div>
						<div className="truncate text-[10px] text-muted-foreground">
							{commit.authorName}
						</div>
					</div>
					<code className="text-[10px] text-muted-foreground">
						{shortHash(commit.hash)}
					</code>
				</li>
			))}
		</ul>
	);
}

function FileList({
	files,
	onOpenFile,
	total,
	truncated,
}: {
	files: BranchComparisonFile[];
	onOpenFile?: (file: BranchComparisonFile) => void;
	total: number;
	truncated: boolean;
}) {
	const parentRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: files.length,
		estimateSize: () => FILE_ROW_HEIGHT,
		gap: 6,
		getScrollElement: () => parentRef.current,
		overscan: 6,
	});
	const measuredRows = virtualizer.getVirtualItems();
	const visibleRows =
		measuredRows.length > 0
			? measuredRows
			: Array.from(
					{ length: Math.min(files.length, INITIAL_FILE_WINDOW) },
					(_, index) => ({ index, start: index * (FILE_ROW_HEIGHT + 6) }),
				);
	if (files.length === 0)
		return (
			<EmptyState
				icon={<FileDiff />}
				message="Both branch tips have the same files."
			/>
		);
	return (
		<div
			className="custom-scrollbar min-h-0 flex-1 overflow-y-auto"
			ref={parentRef}
		>
			<div className="grid gap-2 pr-1">
				<ul
					className="relative"
					style={{ height: `${virtualizer.getTotalSize()}px` }}
				>
					{visibleRows.map((virtualRow) => {
						const file = files[virtualRow.index];
						return file ? (
							<FileRow
								file={file}
								key={file.path}
								onOpen={onOpenFile}
								style={{ transform: `translateY(${virtualRow.start}px)` }}
							/>
						) : null;
					})}
				</ul>
				{truncated ? (
					<p className="text-center text-muted-foreground text-xs">
						Showing 500 of {total.toLocaleString()} changed files.
					</p>
				) : null}
			</div>
		</div>
	);
}

function FileRow({
	file,
	onOpen,
	style,
}: {
	file: BranchComparisonFile;
	onOpen?: (file: BranchComparisonFile) => void;
	style?: CSSProperties;
}) {
	const content = (
		<>
			<span className="w-16 shrink-0 rounded bg-muted px-1.5 py-0.5 text-center text-[9px] uppercase tracking-wide">
				{file.status}
			</span>
			<span className="min-w-0 truncate font-mono text-xs">{file.path}</span>
		</>
	);
	return (
		<li
			className="absolute left-0 right-0 rounded-lg border bg-card"
			style={style}
		>
			{onOpen ? (
				<button
					aria-label={`Open comparison diff for ${file.path}`}
					className="flex w-full items-center gap-2 rounded-lg px-2.5 py-2 text-left transition-colors hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
					onClick={() => onOpen(file)}
					type="button"
				>
					{content}
				</button>
			) : (
				<div className="flex items-center gap-2 px-2.5 py-2">{content}</div>
			)}
		</li>
	);
}

function EmptyState({ icon, message }: { icon: ReactNode; message: string }) {
	return (
		<div className="grid min-h-40 place-items-center rounded-lg border border-dashed bg-muted/20 text-center text-muted-foreground">
			<div className="grid justify-items-center gap-2 [&_svg]:size-5">
				{icon}
				<span className="text-xs">{message}</span>
			</div>
		</div>
	);
}
