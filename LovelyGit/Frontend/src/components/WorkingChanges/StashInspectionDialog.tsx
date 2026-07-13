import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { lazy, Suspense, useState } from "react";
import { Archive, FileSearch } from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import type { RepositoryStashItem } from "@/generated/types";
import { StashInspectionFileList } from "./StashInspectionFileList";
import {
	type StashInspectionFile,
	useStashInspection,
} from "./useStashInspection";

const LazyCommitFileDiff = lazy(() =>
	import("../CommitFileDiff/CommitFileDiffView").then((module) => ({
		default: module.CommitFileDiffView,
	})),
);

type StashInspectionDialogProps = {
	onClose: () => void;
	repositoryId: string;
	stash: RepositoryStashItem | null;
};

export function StashInspectionDialog(props: StashInspectionDialogProps) {
	return (
		<StashInspectionDialogContent
			{...props}
			key={props.stash?.commitHash ?? "closed"}
		/>
	);
}

function StashInspectionDialogContent({
	onClose,
	repositoryId,
	stash,
}: StashInspectionDialogProps) {
	const reduceMotion = useReducedMotion();
	const inspection = useStashInspection(repositoryId, stash);
	const [selected, setSelected] = useState<StashInspectionFile | null>(null);

	return (
		<Dialog onOpenChange={(open) => !open && onClose()} open={stash !== null}>
			<DialogContent className="grid h-[min(760px,calc(100vh-2rem))] w-[min(1240px,calc(100vw-2rem))] max-w-none grid-rows-[auto_minmax(0,1fr)] gap-0 overflow-hidden p-0 sm:max-w-none">
				<DialogHeader className="border-b px-5 py-3 pr-12">
					<DialogTitle className="flex items-center gap-2">
						<Archive aria-hidden="true" className="size-4 text-primary" />
						Inspect {stash?.selector}
					</DialogTitle>
					<DialogDescription className="truncate">
						{stash?.message || "Stashed working changes"} · Read-only
					</DialogDescription>
				</DialogHeader>
				<div className="grid min-h-0 grid-cols-[minmax(230px,320px)_minmax(0,1fr)]">
					<aside className="flex min-h-0 flex-col border-r bg-card/40">
						<InspectionSummary state={inspection.state} />
						{inspection.state.status === "loaded" ? (
							<StashInspectionFileList
								files={inspection.state.files}
								onSelect={setSelected}
								selected={selected}
							/>
						) : null}
						{inspection.state.status === "error" ? (
							<div className="m-3 rounded-md border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">
								<p>{inspection.state.message}</p>
								<Button
									className="mt-3"
									onClick={inspection.retry}
									size="sm"
									variant="outline"
								>
									Retry
								</Button>
							</div>
						) : null}
					</aside>
					<div className="relative min-h-0 min-w-0 overflow-hidden bg-background">
						<AnimatePresence initial={false} mode="wait">
							{selected ? (
								<motion.div
									animate={{ opacity: 1, x: 0 }}
									className="absolute inset-0 flex min-h-0"
									exit={{ opacity: 0, x: reduceMotion ? 0 : 12 }}
									initial={{ opacity: 0, x: reduceMotion ? 0 : 12 }}
									key={`${selected.commitHash}:${selected.file.path}`}
									transition={{ duration: reduceMotion ? 0 : 0.16 }}
								>
									<Suspense fallback={<DiffLoading />}>
										<LazyCommitFileDiff
											commitHash={selected.commitHash}
											file={selected.file}
											onClose={() => setSelected(null)}
											parentIndex={0}
											repositoryId={repositoryId}
										/>
									</Suspense>
								</motion.div>
							) : (
								<EmptyInspection key="empty" />
							)}
						</AnimatePresence>
					</div>
				</div>
			</DialogContent>
		</Dialog>
	);
}

function InspectionSummary({
	state,
}: {
	state: ReturnType<typeof useStashInspection>["state"];
}) {
	if (state.status === "loading" || state.status === "idle") {
		return <div className="h-12 animate-pulse border-b bg-muted/60" />;
	}
	if (state.status !== "loaded") return null;
	const additions =
		state.tracked.stats.additions + (state.untracked?.stats.additions ?? 0);
	const deletions =
		state.tracked.stats.deletions + (state.untracked?.stats.deletions ?? 0);
	return (
		<div className="flex h-12 shrink-0 items-center gap-2 border-b px-3 text-xs text-muted-foreground">
			<span>{state.files.length} files</span>
			<span className="text-emerald-600 dark:text-emerald-400">
				+{additions}
			</span>
			<span className="text-red-600 dark:text-red-400">-{deletions}</span>
			{state.untracked ? (
				<span className="ml-auto">Includes untracked</span>
			) : null}
		</div>
	);
}

function DiffLoading() {
	return (
		<div
			aria-label="Preparing stashed file diff"
			className="grid h-full flex-1 place-items-center text-xs text-muted-foreground"
			role="status"
		>
			Preparing diff…
		</div>
	);
}

function EmptyInspection() {
	return (
		<motion.div
			animate={{ opacity: 1 }}
			className="grid h-full place-items-center p-6 text-center"
			initial={{ opacity: 0 }}
		>
			<div>
				<FileSearch
					aria-hidden="true"
					className="mx-auto size-8 text-primary"
				/>
				<p className="mt-3 font-medium">Choose a stashed file</p>
				<p className="mt-1 text-xs text-muted-foreground">
					Inspect its exact diff without changing the working tree.
				</p>
			</div>
		</motion.div>
	);
}
