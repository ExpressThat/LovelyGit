import { GitBranch } from "lucide-react";
import { CommitSignatureBadge } from "@/components/CommitSignatureBadge";
import type { CommitChangedFile } from "@/generated/types";
import { formatDate, shortHash } from "../CommitGraph/utils/format";
import { CommitDetailsChangedFilesList } from "./CommitDetailsChangedFilesList";
import { CommitDetailsCopyButtons } from "./CommitDetailsCopyButtons";
import { CommitParentSelector } from "./CommitParentSelector";
import { useCommitDetails } from "./useCommitDetails";

export function CommitDetails({
	commitHash,
	onOpenFileBlame,
	onOpenFileHistory,
	onParentIndexChange,
	onSelectFile,
	parentIndex,
	repositoryId,
	refreshToken = 0,
}: {
	commitHash: string;
	onOpenFileBlame: (file: CommitChangedFile) => void;
	onOpenFileHistory: (file: CommitChangedFile) => void;
	onParentIndexChange: (parentIndex: number) => void;
	onSelectFile: (file: CommitChangedFile) => void;
	parentIndex: number;
	repositoryId: string;
	refreshToken?: number;
}) {
	const detailsState = useCommitDetails(
		repositoryId,
		commitHash,
		parentIndex,
		refreshToken,
	);
	const { state } = detailsState;

	if (state.status === "loading") {
		return (
			<div className="space-y-3 p-4">
				<div className="h-4 w-40 animate-pulse rounded bg-muted" />
				<div className="h-20 animate-pulse rounded bg-muted" />
				<div className="h-40 animate-pulse rounded bg-muted" />
			</div>
		);
	}

	if (state.status === "error") {
		return (
			<div className="m-4 rounded-md border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">
				{state.message}
			</div>
		);
	}

	const { details } = state;
	const title = details.subject || "(no commit message)";

	return (
		<div className="space-y-4 p-4 text-left text-sm">
			<section className="space-y-2">
				<div className="flex min-w-0 items-center gap-2 text-xs text-muted-foreground">
					<span className="font-mono">{shortHash(details.hash)}</span>
					<span>{formatDate(details.date)}</span>
					<CommitSignatureBadge kind={details.signatureKind} />
				</div>
				<h3 className="text-base font-semibold leading-snug text-foreground">
					{title}
				</h3>
				<div className="text-xs text-muted-foreground">
					{details.author} &lt;{details.email || "unknown"}&gt;
				</div>
				<CommitDetailsCopyButtons details={details} />
			</section>

			<CommitParentSelector
				busy={state.isRefreshing}
				onChange={onParentIndexChange}
				parents={details.parents}
				selectedIndex={parentIndex}
			/>

			{state.refreshError ? (
				<div className="flex items-center justify-between gap-2 rounded-md border border-destructive/40 bg-destructive/10 p-2 text-destructive text-xs">
					<span>{state.refreshError}</span>
					<button
						className="shrink-0 rounded border border-destructive/40 px-2 py-1 hover:bg-destructive/10"
						onClick={detailsState.retry}
						type="button"
					>
						Retry
					</button>
				</div>
			) : null}

			{details.message ? (
				<pre className="custom-scrollbar max-h-56 overflow-auto whitespace-pre-wrap rounded-md border bg-card p-3 font-mono text-xs leading-5 text-card-foreground">
					{details.message}
				</pre>
			) : null}

			<section className="grid grid-cols-3 gap-2">
				<Stat label="Files" value={details.changedFiles.length} />
				<Stat
					label="Additions"
					value={`+${details.stats.additions.toLocaleString()}`}
				/>
				<Stat
					label="Deletions"
					value={`-${details.stats.deletions.toLocaleString()}`}
				/>
			</section>

			{details.branches.length > 0 || details.tags.length > 0 ? (
				<section className="flex flex-wrap gap-1.5">
					{[...details.branches, ...details.tags].map((ref) => (
						<span
							className="inline-flex max-w-full items-center gap-1 rounded border bg-card px-1.5 py-0.5 text-xs text-muted-foreground"
							key={ref}
							title={ref}
						>
							<GitBranch aria-hidden="true" size={12} />
							<span className="truncate">{ref}</span>
						</span>
					))}
				</section>
			) : null}

			<div
				aria-busy={state.isRefreshing}
				className={state.isRefreshing ? "pointer-events-none opacity-60" : ""}
			>
				<CommitDetailsChangedFilesList
					files={details.changedFiles}
					onOpenBlame={onOpenFileBlame}
					onOpenHistory={onOpenFileHistory}
					onSelectFile={onSelectFile}
				/>
			</div>
		</div>
	);
}

function Stat({ label, value }: { label: string; value: number | string }) {
	return (
		<div className="rounded-md border bg-card p-2">
			<div className="text-[10px] font-semibold uppercase text-muted-foreground">
				{label}
			</div>
			<div className="mt-1 font-mono text-sm text-foreground">{value}</div>
		</div>
	);
}
