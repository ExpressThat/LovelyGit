import {
	AlertTriangle,
	GitCompareArrows,
	LoaderCircle,
	RotateCcw,
} from "lucide-react";
import { AnimatePresence } from "motion/react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import type { BranchComparisonFile, CommitGraphRow } from "@/generated/types";
import { useCommitComparison } from "../hooks/useCommitComparison";
import { shortHash } from "../utils/format";
import {
	BranchComparisonContent,
	type BranchComparisonSection,
} from "./BranchComparisonContent";
import { ComparisonDiffDialog } from "./ComparisonDiffDialog";

export function CommitComparisonDialog({
	base,
	onClose,
	repositoryId,
	target,
}: {
	base: CommitGraphRow;
	onClose: () => void;
	repositoryId: string | null;
	target: CommitGraphRow;
}) {
	const [section, setSection] = useState<BranchComparisonSection>("files");
	const [selectedFile, setSelectedFile] = useState<BranchComparisonFile | null>(
		null,
	);
	const { comparison, error, isLoading, retry } = useCommitComparison(
		repositoryId,
		base.commit.hash,
		target.commit.hash,
	);
	const baseHash = shortHash(base.commit.hash);
	const targetHash = shortHash(target.commit.hash);
	if (selectedFile && repositoryId) {
		return (
			<ComparisonDiffDialog
				baseCommitHash={base.commit.hash}
				file={selectedFile}
				onClose={() => setSelectedFile(null)}
				repositoryId={repositoryId}
				targetCommitHash={target.commit.hash}
			/>
		);
	}
	return (
		<Dialog onOpenChange={(open) => !open && onClose()} open>
			<DialogContent className="flex max-h-[min(84vh,760px)] flex-col sm:max-w-2xl">
				<DialogHeader>
					<DialogTitle className="flex items-center gap-2">
						<GitCompareArrows
							aria-hidden="true"
							className="size-5 text-primary"
						/>
						Compare {baseHash} with {targetHash}
					</DialogTitle>
					<DialogDescription>
						Review unique history and direct tree changes between these exact
						commits.
					</DialogDescription>
				</DialogHeader>
				{isLoading && !comparison ? <Loading /> : null}
				{error ? (
					<div className="flex items-center gap-2 rounded-lg border border-destructive/30 bg-destructive/8 p-3 text-destructive text-xs">
						<span className="min-w-0 flex-1">{error}</span>
						<Button onClick={retry} size="sm" type="button" variant="outline">
							<RotateCcw aria-hidden="true" /> Retry
						</Button>
					</div>
				) : null}
				{comparison ? (
					<>
						<div className="grid grid-cols-3 gap-2">
							<Metric
								active={section === "ahead"}
								label={`${baseHash} only`}
								onClick={() => setSection("ahead")}
								value={comparison.aheadCount}
							/>
							<Metric
								active={section === "behind"}
								label={`${targetHash} only`}
								onClick={() => setSection("behind")}
								value={comparison.behindCount}
							/>
							<Metric
								active={section === "files"}
								label="Changed files"
								onClick={() => setSection("files")}
								value={comparison.changedFileCount}
							/>
						</div>
						{comparison.isHistoryPartial ? <PartialWarning /> : null}
						<AnimatePresence initial={false} mode="wait">
							<BranchComparisonContent
								comparison={comparison}
								onOpenFile={repositoryId ? setSelectedFile : undefined}
								section={section}
							/>
						</AnimatePresence>
					</>
				) : null}
			</DialogContent>
		</Dialog>
	);
}

function Metric({
	active,
	label,
	onClick,
	value,
}: {
	active: boolean;
	label: string;
	onClick: () => void;
	value: number;
}) {
	return (
		<button
			aria-pressed={active}
			className={`rounded-lg border px-3 py-2 text-left transition-colors ${active ? "border-primary/40 bg-primary/10" : "bg-card hover:bg-accent"}`}
			onClick={onClick}
			type="button"
		>
			<span className="block font-semibold text-xl tabular-nums">
				{value.toLocaleString()}
			</span>
			<span className="block truncate text-[10px] text-muted-foreground uppercase tracking-wide">
				{label}
			</span>
		</button>
	);
}
function Loading() {
	return (
		<div className="grid min-h-48 place-items-center text-muted-foreground">
			<div className="grid justify-items-center gap-2">
				<LoaderCircle aria-hidden="true" className="size-5 animate-spin" />
				<span className="text-xs">
					Comparing exact commits with LovelyGit’s native parser…
				</span>
			</div>
		</div>
	);
}
function PartialWarning() {
	return (
		<p className="flex gap-2 rounded-lg border border-amber-500/25 bg-amber-500/8 p-2.5 text-muted-foreground text-xs">
			<AlertTriangle
				aria-hidden="true"
				className="size-4 shrink-0 text-amber-500"
			/>
			History is unusually large, so commit counts are a bounded preview.
		</p>
	);
}
