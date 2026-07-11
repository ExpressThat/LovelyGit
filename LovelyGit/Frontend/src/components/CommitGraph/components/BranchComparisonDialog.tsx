import { AnimatePresence } from "motion/react";
import { useState } from "react";
import {
	AlertTriangle,
	GitCompareArrows,
	GitMerge,
	ListRestart,
	LoaderCircle,
} from "@/components/icons/lovelyIcons";
import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import { Button } from "@/components/ui/button";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import type { BranchComparisonFile } from "@/generated/types";
import { useBranchComparison } from "../hooks/useBranchComparison";
import {
	BranchComparisonContent,
	type BranchComparisonSection,
} from "./BranchComparisonContent";
import { ComparisonDiffDialog } from "./ComparisonDiffDialog";

export function BranchComparisonDialog({
	currentBranchName,
	onClose,
	onIntegrate,
	repositoryId,
	targetBranchName,
}: {
	currentBranchName: string | null;
	onClose: () => void;
	onIntegrate: (mode: BranchIntegrationMode, branchName: string) => void;
	repositoryId: string | null;
	targetBranchName: string;
}) {
	const [section, setSection] = useState<BranchComparisonSection>("ahead");
	const [selectedFile, setSelectedFile] = useState<BranchComparisonFile | null>(
		null,
	);
	const { comparison, error, isLoading } = useBranchComparison(
		repositoryId,
		targetBranchName,
	);
	const integrate = (mode: BranchIntegrationMode) => {
		onClose();
		onIntegrate(mode, targetBranchName);
	};
	if (selectedFile && repositoryId && comparison) {
		return (
			<ComparisonDiffDialog
				baseCommitHash={comparison.currentHash}
				file={selectedFile}
				onClose={() => setSelectedFile(null)}
				repositoryId={repositoryId}
				targetCommitHash={comparison.targetHash}
			/>
		);
	}
	return (
		<Dialog onOpenChange={(open) => !open && onClose()} open>
			<DialogContent className="flex max-h-[min(84vh,760px)] flex-col sm:max-w-2xl">
				<DialogHeader>
					<DialogTitle className="flex items-center gap-2">
						<GitCompareArrows className="size-5 text-primary" />
						Compare {currentBranchName} with {targetBranchName}
					</DialogTitle>
					<DialogDescription>
						Review divergence and tip-to-tip file changes before integrating
						either branch.
					</DialogDescription>
				</DialogHeader>
				{isLoading ? <Loading /> : null}
				{error ? (
					<p className="rounded-lg border border-destructive/30 bg-destructive/8 p-3 text-destructive text-xs">
						{error}
					</p>
				) : null}
				{comparison ? (
					<>
						<div className="grid grid-cols-3 gap-2">
							<Metric
								active={section === "ahead"}
								label={`${comparison.currentBranchName} ahead`}
								onClick={() => setSection("ahead")}
								value={comparison.aheadCount}
							/>
							<Metric
								active={section === "behind"}
								label={`${comparison.targetBranchName} ahead`}
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
						{comparison.isHistoryPartial ? (
							<p className="flex gap-2 rounded-lg border border-amber-500/25 bg-amber-500/8 p-2.5 text-muted-foreground text-xs">
								<AlertTriangle className="size-4 shrink-0 text-amber-500" />
								History is unusually large, so commit counts are a bounded
								preview.
							</p>
						) : null}
						<AnimatePresence initial={false} mode="wait">
							<BranchComparisonContent
								comparison={comparison}
								onOpenFile={repositoryId ? setSelectedFile : undefined}
								section={section}
							/>
						</AnimatePresence>
						<DialogFooter className="m-0 border-t px-0 pt-3">
							<Button
								onClick={() => integrate("rebase")}
								type="button"
								variant="outline"
							>
								<ListRestart />
								Rebase {comparison.currentBranchName} onto{" "}
								{comparison.targetBranchName}
							</Button>
							<Button onClick={() => integrate("merge")} type="button">
								<GitMerge />
								Merge {comparison.targetBranchName} into{" "}
								{comparison.currentBranchName}
							</Button>
						</DialogFooter>
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
				<LoaderCircle className="size-5 animate-spin" />
				<span className="text-xs">
					Painting both histories with LovelyGit’s native parser…
				</span>
			</div>
		</div>
	);
}
