import { useVirtualizer } from "@tanstack/react-virtual";
import { Columns2, Rows3, WrapText, X } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import type {
	CommitChangedFile,
	CommitFileDiffChangeSpan,
	CommitFileDiffLine,
	CommitFileDiffResponse,
	CommitFileDiffSyntaxSpan,
} from "@/generated/ExpressThat.LovelyGit.Services.Git.CommitGraph.Models";
import { sendRequestWithResponse } from "@/lib/registerSignalR";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";

type DiffState =
	| { status: "loading" }
	| { status: "error"; message: string }
	| { status: "loaded"; diff: CommitFileDiffResponse };

const LOADING_DIFF_ROWS = Array.from({ length: 16 }, (_, index) => ({
	id: `loading-diff-row-${index}`,
	width: index % 3 === 0 ? 72 : 96,
}));
const DIFF_OVERSCAN = 12;

export function CommitFileDiffView({
	commitHash,
	file,
	onClose,
	repositoryId,
}: {
	commitHash: string;
	file: CommitChangedFile;
	onClose: () => void;
	repositoryId: string;
}) {
	const viewMode = useSetting("CommitDiffViewMode");
	const wrapLines = useSetting("CommitDiffWrapLines");
	const [state, setState] = useState<DiffState>({ status: "loading" });

	useEffect(() => {
		let isActive = true;
		setState({ status: "loading" });

		sendRequestWithResponse({
			commandType: "GetCommitFileDiff",
			arguments: {
				commitHash,
				path: file.path,
				repositoryId,
				viewMode,
			},
		})
			.then((diff) => {
				if (!isActive) {
					return;
				}

				if (!diff) {
					setState({ status: "error", message: "File diff was empty." });
					return;
				}

				setState({ status: "loaded", diff });
			})
			.catch((error: unknown) => {
				if (!isActive) {
					return;
				}

				setState({
					status: "error",
					message:
						error instanceof Error
							? error.message
							: "Failed to load file diff.",
				});
			});

		return () => {
			isActive = false;
		};
	}, [commitHash, file.path, repositoryId, viewMode]);

	const updateViewMode = (nextViewMode: typeof viewMode) => {
		void setSetting("CommitDiffViewMode", nextViewMode);
	};

	const updateWrapLines = (nextWrapLines: boolean) => {
		void setSetting("CommitDiffWrapLines", nextWrapLines);
	};

	const handleClose = () => {
		setState({ status: "loading" });
		onClose();
	};

	return (
		<section className="flex h-full min-w-0 flex-1 flex-col overflow-hidden border-l bg-background text-foreground">
			<header className="shrink-0 border-b bg-popover text-popover-foreground">
				<div className="flex h-10 items-center gap-2 px-3">
					<div className="min-w-0 flex-1 truncate font-mono text-sm text-muted-foreground">
						<span>{folderPrefix(file.path)}</span>
						<span className="font-semibold text-foreground">
							{fileName(file.path)}
						</span>
					</div>
					<div className="hidden items-center gap-2 text-[10px] uppercase text-muted-foreground md:flex">
						<span>{file.status}</span>
						<span>
							+{file.additions} -{file.deletions}
						</span>
					</div>
					<button
						aria-label="Close diff"
						className="inline-flex size-7 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground"
						onClick={handleClose}
						type="button"
					>
						<X aria-hidden="true" size={16} />
					</button>
				</div>
				<div className="flex h-10 items-center justify-center border-t bg-card/60 px-3">
					<div className="inline-flex rounded-md border bg-background p-0.5">
						<ModeButton
							icon={<Columns2 aria-hidden="true" size={14} />}
							isActive={viewMode === "SideBySide"}
							label="Side by side"
							onClick={() => updateViewMode("SideBySide")}
						/>
						<ModeButton
							icon={<Rows3 aria-hidden="true" size={14} />}
							isActive={viewMode === "Combined"}
							label="Combined"
							onClick={() => updateViewMode("Combined")}
						/>
					</div>
					<div className="ml-2 inline-flex rounded-md border bg-background p-0.5">
						<ModeButton
							icon={<WrapText aria-hidden="true" size={14} />}
							isActive={wrapLines}
							label="Wrap lines"
							onClick={() => updateWrapLines(!wrapLines)}
						/>
					</div>
				</div>
			</header>
			<div className="min-h-0 flex-1 overflow-hidden bg-background">
				{state.status === "loading" ? <LoadingDiff /> : null}
				{state.status === "error" ? (
					<div className="m-4 rounded-md border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">
						{state.message}
					</div>
				) : null}
				{state.status === "loaded" ? (
					<DiffContent diff={state.diff} wrapLines={wrapLines} />
				) : null}
			</div>
		</section>
	);
}

function ModeButton({
	icon,
	isActive,
	label,
	onClick,
}: {
	icon: React.ReactNode;
	isActive: boolean;
	label: string;
	onClick: () => void;
}) {
	return (
		<button
			aria-label={label}
			className={`inline-flex h-7 items-center gap-1 rounded px-2 text-xs text-muted-foreground hover:bg-accent hover:text-accent-foreground ${
				isActive ? "bg-accent font-semibold text-accent-foreground" : ""
			}`}
			onClick={onClick}
			title={label}
			type="button"
		>
			{icon}
			<span>{label}</span>
		</button>
	);
}

function LoadingDiff() {
	return (
		<div className="space-y-2 p-4">
			{LOADING_DIFF_ROWS.map((row) => (
				<div
					className="h-5 animate-pulse rounded bg-muted"
					key={row.id}
					style={{ width: `${row.width}%` }}
				/>
			))}
		</div>
	);
}

export function DiffContent({
	diff,
	wrapLines,
}: {
	diff: CommitFileDiffResponse;
	wrapLines: boolean;
}) {
	if (diff.isBinary) {
		return (
			<div className="m-4 rounded-md border bg-card p-4 text-sm text-muted-foreground">
				Binary file diff is not available.
			</div>
		);
	}

	if (diff.lines.length === 0 || !diff.hasDifferences) {
		return (
			<div className="m-4 rounded-md border bg-card p-4 text-sm text-muted-foreground">
				No textual differences.
			</div>
		);
	}

	return diff.viewMode === "SideBySide" ? (
		<SideBySideDiff lines={diff.lines} wrapLines={wrapLines} />
	) : (
		<CombinedDiff lines={diff.lines} wrapLines={wrapLines} />
	);
}

function SideBySideDiff({
	lines,
	wrapLines,
}: {
	lines: CommitFileDiffLine[];
	wrapLines: boolean;
}) {
	const viewportRef = useRef<HTMLDivElement | null>(null);
	const oldScrollerRef = useRef<HTMLDivElement | null>(null);
	const newScrollerRef = useRef<HTMLDivElement | null>(null);
	const syncingRef = useRef(false);
	const [oldScrollLeft, setOldScrollLeft] = useState(0);
	const [newScrollLeft, setNewScrollLeft] = useState(0);
	const contentWidth = useMemo(
		() =>
			estimateCodeWidth(
				lines.flatMap((line) => [line.oldText ?? "", line.newText ?? ""]),
			),
		[lines],
	);
	const virtualizer = useVirtualizer({
		count: lines.length,
		estimateSize: () => 18,
		getScrollElement: () => viewportRef.current,
		measureElement: (element) => element.getBoundingClientRect().height,
		overscan: DIFF_OVERSCAN,
	});
	const virtualItems = virtualizer.getVirtualItems();

	const syncBottomScroll = (
		source: "old" | "new",
		event: React.UIEvent<HTMLDivElement>,
	) => {
		if (syncingRef.current) {
			return;
		}

		const nextScrollLeft = event.currentTarget.scrollLeft;
		const target =
			source === "old" ? newScrollerRef.current : oldScrollerRef.current;
		if (!target) {
			return;
		}

		syncingRef.current = true;
		target.scrollLeft = nextScrollLeft;
		setOldScrollLeft(nextScrollLeft);
		setNewScrollLeft(nextScrollLeft);
		requestAnimationFrame(() => {
			syncingRef.current = false;
		});
	};

	return (
		<div className="flex h-full min-w-0 flex-col font-mono text-[12px] leading-[18px] text-foreground">
			<div className="grid shrink-0 grid-cols-2 border-b bg-card text-[10px] font-semibold uppercase text-muted-foreground">
				<DiffPaneHeader headerLabel="Before" lineNumberLabel="Old" />
				<DiffPaneHeader headerLabel="After" lineNumberLabel="New" />
			</div>
			<div
				className="custom-scrollbar relative min-h-0 flex-1 overflow-x-hidden overflow-y-auto"
				ref={viewportRef}
			>
				<div
					className="relative w-full"
					style={{ height: `${virtualizer.getTotalSize()}px` }}
				>
					<div aria-hidden="true" className="pointer-events-none invisible">
						{virtualItems.map((item) => {
							const line = lines[item.index];
							return (
								<div
									className="absolute left-0 top-0 grid w-full grid-cols-2"
									data-index={item.index}
									key={`measure:${item.key}`}
									ref={virtualizer.measureElement}
									style={{ transform: `translateY(${item.start}px)` }}
								>
									<SideBySideRow
										line={line}
										scrollLeft={0}
										side="old"
										width={contentWidth}
										wrapLines={wrapLines}
									/>
									<SideBySideRow
										line={line}
										scrollLeft={0}
										side="new"
										width={contentWidth}
										wrapLines={wrapLines}
									/>
								</div>
							);
						})}
					</div>
					<div className="absolute inset-0 grid grid-cols-2">
						<div className="relative min-w-0 border-r">
							{virtualItems.map((item) => {
								const line = lines[item.index];
								return (
									<div
										className="absolute left-0 top-0 w-full"
										key={`old:${item.key}`}
										style={{ transform: `translateY(${item.start}px)` }}
									>
										<SideBySideRow
											line={line}
											rowHeight={item.size}
											scrollLeft={oldScrollLeft}
											side="old"
											width={contentWidth}
											wrapLines={wrapLines}
										/>
									</div>
								);
							})}
						</div>
						<div className="relative min-w-0">
							{virtualItems.map((item) => {
								const line = lines[item.index];
								return (
									<div
										className="absolute left-0 top-0 w-full"
										key={`new:${item.key}`}
										style={{ transform: `translateY(${item.start}px)` }}
									>
										<SideBySideRow
											line={line}
											rowHeight={item.size}
											scrollLeft={newScrollLeft}
											side="new"
											width={contentWidth}
											wrapLines={wrapLines}
										/>
									</div>
								);
							})}
						</div>
					</div>
				</div>
			</div>
			{wrapLines ? null : (
				<div className="grid h-3 shrink-0 grid-cols-2 border-t bg-background">
					<div
						className="custom-scrollbar overflow-x-auto overflow-y-hidden border-r"
						onScroll={(event) => syncBottomScroll("old", event)}
						ref={oldScrollerRef}
					>
						<div style={{ height: 1, width: contentWidth }} />
					</div>
					<div
						className="custom-scrollbar overflow-x-auto overflow-y-hidden"
						onScroll={(event) => syncBottomScroll("new", event)}
						ref={newScrollerRef}
					>
						<div style={{ height: 1, width: contentWidth }} />
					</div>
				</div>
			)}
		</div>
	);
}

function DiffPaneHeader({
	headerLabel,
	lineNumberLabel,
}: {
	headerLabel: string;
	lineNumberLabel: string;
}) {
	return (
		<div className="grid grid-cols-[4rem_minmax(0,1fr)] border-r last:border-r-0">
			<div className="border-r px-2 py-1 text-right">{lineNumberLabel}</div>
			<div className="px-2 py-1">{headerLabel}</div>
		</div>
	);
}

function SideBySideRow({
	line,
	rowHeight,
	scrollLeft,
	side,
	width,
	wrapLines,
}: {
	line: CommitFileDiffLine;
	rowHeight?: number;
	scrollLeft: number;
	side: "old" | "new";
	width: number;
	wrapLines: boolean;
}) {
	const isOld = side === "old";
	return (
		<div
			className={`grid min-w-0 select-text grid-cols-[4rem_minmax(0,1fr)] ${lineBackground(line.changeType)}`}
			style={rowHeight === undefined ? undefined : { minHeight: rowHeight }}
		>
			<LineNumber value={isOld ? line.oldLineNumber : line.newLineNumber} />
			<CodeCell
				changeSpans={isOld ? line.oldChangeSpans : line.newChangeSpans}
				scrollLeft={scrollLeft}
				spans={isOld ? line.oldSyntaxSpans : line.newSyntaxSpans}
				text={isOld ? line.oldText : line.newText}
				variant={
					isOld
						? oldSideVariant(line.changeType)
						: newSideVariant(line.changeType)
				}
				width={width}
				wrapLines={wrapLines}
			/>
		</div>
	);
}

function CombinedDiff({
	lines,
	wrapLines,
}: {
	lines: CommitFileDiffLine[];
	wrapLines: boolean;
}) {
	const viewportRef = useRef<HTMLDivElement | null>(null);
	const scrollerRef = useRef<HTMLDivElement | null>(null);
	const [scrollLeft, setScrollLeft] = useState(0);
	const contentWidth = useMemo(
		() => estimateCodeWidth(lines.map((line) => line.text ?? "")),
		[lines],
	);
	const virtualizer = useVirtualizer({
		count: lines.length,
		estimateSize: () => 18,
		getScrollElement: () => viewportRef.current,
		measureElement: (element) => element.getBoundingClientRect().height,
		overscan: DIFF_OVERSCAN,
	});
	const virtualItems = virtualizer.getVirtualItems();

	return (
		<div className="flex h-full min-w-0 flex-col font-mono text-[12px] leading-[18px] text-foreground">
			<div className="grid shrink-0 grid-cols-[4rem_4rem_2rem_minmax(0,1fr)] border-b bg-card text-[10px] font-semibold uppercase text-muted-foreground">
				<div className="border-r px-2 py-1 text-right">Old</div>
				<div className="border-r px-2 py-1 text-right">New</div>
				<div className="border-r px-2 py-1 text-center"> </div>
				<div className="px-2 py-1">Code</div>
			</div>
			<div
				className="custom-scrollbar relative min-h-0 flex-1 overflow-x-hidden overflow-y-auto"
				ref={viewportRef}
			>
				<div
					className="relative w-full"
					style={{ height: `${virtualizer.getTotalSize()}px` }}
				>
					{virtualItems.map((item) => {
						const line = lines[item.index];
						return (
							<div
								className={`absolute left-0 top-0 grid w-full grid-cols-[4rem_4rem_2rem_minmax(0,1fr)] ${lineBackground(line.changeType)}`}
								data-index={item.index}
								key={item.key}
								ref={virtualizer.measureElement}
								style={{ transform: `translateY(${item.start}px)` }}
							>
								<LineNumber value={line.oldLineNumber} />
								<LineNumber value={line.newLineNumber} />
								<div className="border-r px-2 text-center text-muted-foreground">
									{changeMarker(line.changeType)}
								</div>
								<CodeCell
									changeSpans={line.changeSpans}
									scrollLeft={scrollLeft}
									spans={line.syntaxSpans}
									text={line.text}
									variant={
										line.changeType === "Deleted"
											? "deleted"
											: line.changeType === "Inserted"
												? "inserted"
												: "plain"
									}
									width={contentWidth}
									wrapLines={wrapLines}
								/>
							</div>
						);
					})}
				</div>
			</div>
			{wrapLines ? null : (
				<div
					className="custom-scrollbar h-3 shrink-0 overflow-x-auto overflow-y-hidden border-t bg-background"
					onScroll={(event) => setScrollLeft(event.currentTarget.scrollLeft)}
					ref={scrollerRef}
				>
					<div style={{ height: 1, width: contentWidth }} />
				</div>
			)}
		</div>
	);
}

function LineNumber({ value }: { value?: number | null }) {
	return (
		<div className="select-none border-r bg-card/45 px-2 text-right text-muted-foreground">
			{value ?? ""}
		</div>
	);
}

function CodeCell({
	changeSpans,
	scrollLeft,
	spans,
	text,
	variant,
	width,
	wrapLines,
}: {
	changeSpans: CommitFileDiffChangeSpan[];
	scrollLeft: number;
	spans: CommitFileDiffSyntaxSpan[];
	text: string;
	variant: "deleted" | "inserted" | "plain";
	width: number;
	wrapLines: boolean;
}) {
	return (
		<div
			className={`min-h-[18px] overflow-hidden border-r px-2 ${
				wrapLines ? "whitespace-pre-wrap break-all" : "whitespace-pre"
			} ${codeVariantClass(variant)}`}
		>
			<div
				style={
					wrapLines
						? undefined
						: {
								transform: `translateX(-${scrollLeft}px)`,
								width,
							}
				}
			>
				{renderSyntaxLine(text, spans, changeSpans, variant)}
			</div>
		</div>
	);
}

function estimateCodeWidth(lines: string[]) {
	const longestLineLength = lines.reduce(
		(longest, line) => Math.max(longest, line.length),
		0,
	);
	return Math.min(48_000, Math.max(1_200, longestLineLength * 7.25 + 32));
}

function renderSyntaxLine(
	text: string,
	spans: CommitFileDiffSyntaxSpan[],
	changeSpans: CommitFileDiffChangeSpan[],
	variant: "deleted" | "inserted" | "plain",
): React.ReactNode {
	if (text.length === 0) {
		return "\u00a0";
	}

	const boundaries = new Set<number>([0, text.length]);
	for (const span of spans) {
		boundaries.add(Math.min(Math.max(span.start, 0), text.length));
		boundaries.add(
			Math.min(Math.max(span.start + span.length, 0), text.length),
		);
	}
	for (const span of changeSpans) {
		boundaries.add(Math.min(Math.max(span.start, 0), text.length));
		boundaries.add(
			Math.min(Math.max(span.start + span.length, 0), text.length),
		);
	}

	const sortedBoundaries = Array.from(boundaries).sort(
		(left, right) => left - right,
	);
	const nodes: React.ReactNode[] = [];
	for (let index = 0; index < sortedBoundaries.length - 1; index++) {
		const start = sortedBoundaries[index];
		const end = sortedBoundaries[index + 1];
		if (end <= start) {
			continue;
		}

		const syntax = findCoveringSyntaxSpan(start, spans);
		const change = findCoveringChangeSpan(start, changeSpans);
		nodes.push(
			<span
				className={[
					syntax ? syntaxClass(syntax.scope) : "",
					change ? changeClass(change.changeType, variant) : "",
				]
					.filter(Boolean)
					.join(" ")}
				key={`${start}:${end}`}
			>
				{text.slice(start, end)}
			</span>,
		);
	}

	return nodes.length > 0 ? nodes : text;
}

function findCoveringSyntaxSpan(
	offset: number,
	spans: CommitFileDiffSyntaxSpan[],
) {
	return spans.find(
		(span) => offset >= span.start && offset < span.start + span.length,
	);
}

function findCoveringChangeSpan(
	offset: number,
	spans: CommitFileDiffChangeSpan[],
) {
	return spans.find(
		(span) => offset >= span.start && offset < span.start + span.length,
	);
}

function lineBackground(changeType: string) {
	switch (changeType) {
		case "Deleted":
			return "bg-red-500/8";
		case "Inserted":
			return "bg-emerald-500/8";
		default:
			return "";
	}
}

function oldSideVariant(changeType: string) {
	return changeType === "Deleted" || changeType === "Modified"
		? "deleted"
		: "plain";
}

function newSideVariant(changeType: string) {
	return changeType === "Inserted" || changeType === "Modified"
		? "inserted"
		: "plain";
}

function codeVariantClass(variant: "deleted" | "inserted" | "plain") {
	switch (variant) {
		case "deleted":
			return "bg-red-500/10";
		case "inserted":
			return "bg-emerald-500/10";
		default:
			return "";
	}
}

function changeClass(
	changeType: string,
	variant: "deleted" | "inserted" | "plain",
) {
	switch (changeType) {
		case "Deleted":
			return "rounded-sm bg-red-500/25 text-foreground";
		case "Inserted":
			return "rounded-sm bg-emerald-500/25 text-foreground";
		case "Modified":
			return variant === "deleted"
				? "rounded-sm bg-red-500/25 text-foreground"
				: "rounded-sm bg-emerald-500/25 text-foreground";
		default:
			return "";
	}
}

function changeMarker(changeType: string) {
	switch (changeType) {
		case "Deleted":
			return "-";
		case "Inserted":
			return "+";
		default:
			return "";
	}
}

function syntaxClass(scope: string) {
	switch (scope) {
		case "Keyword":
			return "text-blue-600 dark:text-blue-300";
		case "String":
		case "StringEscape":
			return "text-emerald-700 dark:text-emerald-300";
		case "Comment":
			return "text-muted-foreground italic";
		case "Number":
			return "text-amber-700 dark:text-amber-300";
		case "Operator":
			return "text-foreground";
		case "Type":
		case "TypeParameter":
			return "text-cyan-700 dark:text-cyan-300";
		case "Name":
		case "Function":
			return "text-violet-700 dark:text-violet-300";
		default:
			return "";
	}
}

function folderPrefix(path: string) {
	const lastSlash = Math.max(path.lastIndexOf("/"), path.lastIndexOf("\\"));
	return lastSlash >= 0 ? path.slice(0, lastSlash + 1) : "";
}

function fileName(path: string) {
	const lastSlash = Math.max(path.lastIndexOf("/"), path.lastIndexOf("\\"));
	return lastSlash >= 0 ? path.slice(lastSlash + 1) : path;
}
