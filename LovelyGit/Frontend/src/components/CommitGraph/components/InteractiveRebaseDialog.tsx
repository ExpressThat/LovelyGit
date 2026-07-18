import { toast } from "sonner";
import {
	AlertTriangle,
	ListTree,
	LoaderCircle,
} from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import type { CommitGraphRow } from "@/generated/types";
import { useInteractiveRebasePlan } from "../hooks/useInteractiveRebasePlan";
import { shortHash } from "../utils/format";
import { InteractiveRebasePlanList } from "./InteractiveRebasePlanList";

export function InteractiveRebaseDialog({
	baseCommit,
	currentBranchName,
	onOpenChange,
	onOpenWorkingChanges,
	onRepositoryChanged,
	repositoryId,
}: {
	baseCommit: CommitGraphRow | null;
	currentBranchName: string | null;
	onOpenChange: (commit: CommitGraphRow | null) => void;
	onOpenWorkingChanges: () => void;
	onRepositoryChanged: () => void;
	repositoryId: string | null;
}) {
	const controller = useInteractiveRebasePlan(
		repositoryId,
		baseCommit?.commit.hash ?? null,
	);
	if (!baseCommit) return null;
	const commitByHash = new Map(
		controller.response?.commits.map((commit) => [commit.hash, commit]),
	);
	const run = async () => {
		const toastId = toast.loading(
			`Rebasing ${currentBranchName ?? "current branch"}`,
		);
		try {
			const outcome = await controller.start();
			if (!outcome) return;
			onOpenChange(null);
			onRepositoryChanged();
			if (outcome.isCompleted) {
				toast.success("Interactive rebase completed", { id: toastId });
			} else {
				toast.warning("Interactive rebase paused for conflicts", {
					description:
						outcome.message ??
						"Resolve and stage the conflicts, then continue or abort.",
					id: toastId,
				});
				onOpenWorkingChanges();
			}
		} catch (reason) {
			toast.error(
				reason instanceof Error ? reason.message : "Interactive rebase failed.",
				{ id: toastId },
			);
		}
	};

	return (
		<Dialog
			open
			onOpenChange={(open) =>
				!open && !controller.isRunning && onOpenChange(null)
			}
		>
			<DialogContent className="h-[min(88vh,760px)] sm:max-w-2xl">
				<form
					className="flex h-full min-h-0 flex-col gap-4"
					onSubmit={(event) => {
						event.preventDefault();
						void run();
					}}
				>
					<DialogHeader>
						<DialogTitle>Clean up {currentBranchName} history</DialogTitle>
						<DialogDescription>
							Edit commits after {shortHash(baseCommit.commit.hash)}. The list
							runs oldest to newest—the order Git will replay it.
						</DialogDescription>
					</DialogHeader>
					<div className="grid grid-cols-5 gap-1 rounded-lg border bg-muted/35 p-2 text-center text-[10px] text-muted-foreground">
						<span>
							<b className="block text-foreground">Pick</b>keep
						</span>
						<span>
							<b className="block text-foreground">Reword</b>rename
						</span>
						<span>
							<b className="block text-foreground">Squash</b>combine messages
						</span>
						<span>
							<b className="block text-foreground">Fixup</b>combine quietly
						</span>
						<span>
							<b className="block text-foreground">Drop</b>remove
						</span>
					</div>
					{controller.isLoading ? <LoadingPlan /> : null}
					{controller.error ? <Notice message={controller.error} /> : null}
					{controller.plan.length > 0 ? (
						<InteractiveRebasePlanList
							commitByHash={commitByHash}
							onAction={controller.updateAction}
							onMessage={controller.updateMessage}
							onMove={controller.move}
							plan={controller.plan}
						/>
					) : null}
					{controller.validationError ? (
						<Notice message={controller.validationError} />
					) : null}
					<p className="flex gap-2 rounded-lg border border-amber-500/25 bg-amber-500/8 p-2.5 text-muted-foreground text-xs">
						<AlertTriangle className="mt-0.5 size-4 shrink-0 text-amber-500" />
						This rewrites commit hashes. Push with care if this branch has
						already been shared.
					</p>
					<DialogFooter className="m-0 p-0">
						<Button
							disabled={
								controller.isLoading ||
								controller.isRunning ||
								Boolean(controller.error || controller.validationError)
							}
							type="submit"
						>
							{controller.isRunning ? (
								<LoaderCircle className="animate-spin" />
							) : (
								<ListTree />
							)}
							{controller.isRunning
								? "Rebasing"
								: `Rebase ${controller.plan.length} commits`}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}

function LoadingPlan() {
	return (
		<div className="grid min-h-28 place-items-center text-muted-foreground">
			<LoaderCircle className="size-5 animate-spin" />
			<span className="text-xs">
				Reading history with LovelyGit’s native parser…
			</span>
		</div>
	);
}
function Notice({ message }: { message: string }) {
	return (
		<p className="rounded-lg border border-destructive/30 bg-destructive/8 p-2.5 text-destructive text-xs">
			{message}
		</p>
	);
}
