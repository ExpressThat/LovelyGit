import { useEffect, useState } from "react";
import { Archive, LoaderCircle } from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogHeader,
	DialogTitle,
	DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { type RepositoryStashItem, StashAction } from "@/generated/types";
import { BranchFromStashDialog } from "./BranchFromStashDialog";
import { DropStashDialog } from "./DropStashDialog";
import { StashInspectionDialog } from "./StashInspectionDialog";
import { StashList } from "./StashList";
import { StashScopeControl } from "./StashScopeControl";
import { useStashDialog } from "./useStashDialog";

export function StashDialog({
	canCreate,
	onOpenChange,
	onRepositoryChanged,
	open: controlledOpen,
	repositoryId,
	selectedPaths = [],
	showTrigger = true,
}: {
	canCreate: boolean;
	onOpenChange?: (open: boolean) => void;
	onRepositoryChanged: () => Promise<void> | void;
	open?: boolean;
	repositoryId: string;
	selectedPaths?: string[];
	showTrigger?: boolean;
}) {
	const [selectedOnly, setSelectedOnly] = useState(
		() => selectedPaths.length > 0,
	);
	const controlled =
		controlledOpen === undefined || onOpenChange === undefined
			? undefined
			: { onOpenChange, open: controlledOpen };
	const stash = useStashDialog(
		repositoryId,
		onRepositoryChanged,
		controlled,
		selectedOnly,
		selectedOnly ? selectedPaths : [],
	);
	const [inspectionTarget, setInspectionTarget] =
		useState<RepositoryStashItem | null>(null);
	const {
		branchNames,
		branchTarget,
		busyAction,
		dropTarget,
		includeUntracked,
		isLoading,
		loadError,
		message,
		open,
		restoreIndex,
		runAction,
		setDropTarget,
		setBranchTarget,
		setIncludeUntracked,
		setMessage,
		setOpen,
		setRestoreIndex,
		stashes,
	} = stash;
	useEffect(() => {
		if (!open) setSelectedOnly(selectedPaths.length > 0);
	}, [open, selectedPaths.length]);

	return (
		<>
			<Dialog open={open} onOpenChange={setOpen}>
				{showTrigger ? (
					<DialogTrigger
						render={
							<Button
								className="h-7 gap-1.5 px-2 text-xs"
								size="sm"
								variant="outline"
							/>
						}
					>
						<Archive aria-hidden="true" className="size-3.5" />
						Stash
					</DialogTrigger>
				) : null}
				<DialogContent className="grid max-h-[min(680px,calc(100vh-2rem))] grid-rows-[auto_auto_minmax(0,1fr)] gap-0 overflow-hidden p-0 sm:max-w-xl">
					<DialogHeader className="border-b px-5 py-4">
						<DialogTitle>Stashes</DialogTitle>
						<DialogDescription>
							Set work aside without committing, then restore it when you are
							ready.
						</DialogDescription>
					</DialogHeader>
					<section className="grid gap-3 border-b bg-card/40 px-5 py-4">
						<div className="grid gap-2">
							<label className="text-sm font-medium" htmlFor="stash-message">
								Message{" "}
								<span className="font-normal text-muted-foreground">
									optional
								</span>
							</label>
							<Input
								aria-label="Message optional"
								id="stash-message"
								onChange={(event) => setMessage(event.currentTarget.value)}
								onInput={(event) => setMessage(event.currentTarget.value)}
								placeholder="What are you setting aside?"
								value={message}
							/>
						</div>
						<StashScopeControl
							onSelectedOnlyChange={setSelectedOnly}
							selectedCount={selectedPaths.length}
							selectedOnly={selectedOnly}
						/>
						<div className="flex flex-wrap items-center justify-between gap-3">
							<label
								className="flex items-center gap-2 text-sm"
								htmlFor="stash-untracked"
							>
								<Checkbox
									aria-label="Include untracked files"
									checked={includeUntracked}
									id="stash-untracked"
									onCheckedChange={setIncludeUntracked}
								/>
								Include untracked files
							</label>
							<Button
								disabled={
									!canCreate ||
									busyAction !== null ||
									(selectedOnly && selectedPaths.length === 0)
								}
								onClick={() => void runAction(StashAction.Create)}
								size="sm"
							>
								{busyAction === StashAction.Create ? (
									<LoaderCircle aria-hidden="true" className="animate-spin" />
								) : (
									<Archive aria-hidden="true" />
								)}
								Stash changes
							</Button>
						</div>
					</section>
					<section className="custom-scrollbar min-h-0 overflow-y-auto px-5 py-4">
						<div className="mb-3 flex items-center justify-between gap-3">
							<div>
								<h3 className="font-medium">Saved stashes</h3>
								<p className="text-xs text-muted-foreground">
									Read directly from the repository reflog
								</p>
							</div>
							<label
								className="flex items-center gap-2 text-xs"
								htmlFor="stash-index"
							>
								<Checkbox
									aria-label="Restore staged state"
									checked={restoreIndex}
									id="stash-index"
									onCheckedChange={setRestoreIndex}
								/>
								Restore staged state
							</label>
						</div>
						<StashList
							busyAction={busyAction}
							isLoading={isLoading}
							loadError={loadError}
							onApply={(item) => void runAction(StashAction.Apply, item)}
							onBranch={setBranchTarget}
							onDrop={setDropTarget}
							onInspect={setInspectionTarget}
							onPop={(item) => void runAction(StashAction.Pop, item)}
							stashes={stashes}
						/>
					</section>
				</DialogContent>
			</Dialog>
			<DropStashDialog
				busyAction={busyAction}
				onConfirm={(target) => void runAction(StashAction.Drop, target)}
				onTargetChange={setDropTarget}
				target={dropTarget}
			/>
			<BranchFromStashDialog
				branchNames={branchNames}
				isBusy={busyAction === StashAction.Branch}
				onClose={() => setBranchTarget(null)}
				onConfirm={(branchName) =>
					void runAction(
						StashAction.Branch,
						branchTarget ?? undefined,
						branchName,
					)
				}
				stash={branchTarget}
			/>
			<StashInspectionDialog
				onClose={() => setInspectionTarget(null)}
				repositoryId={repositoryId}
				stash={inspectionTarget}
			/>
		</>
	);
}
