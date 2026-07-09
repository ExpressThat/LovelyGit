import {
	Archive,
	ArchiveRestore,
	LoaderCircle,
	PackageOpen,
	Trash2,
} from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import {
	AlertDialog,
	AlertDialogAction,
	AlertDialogCancel,
	AlertDialogContent,
	AlertDialogDescription,
	AlertDialogFooter,
	AlertDialogHeader,
	AlertDialogMedia,
	AlertDialogTitle,
} from "@/components/ui/alert-dialog";
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
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { NativeMessageType } from "@/lib/nativeMessaging";

export function StashDialog({
	canCreate,
	onRepositoryChanged,
	repositoryId,
}: {
	canCreate: boolean;
	onRepositoryChanged: () => Promise<void> | void;
	repositoryId: string;
}) {
	const [busyAction, setBusyAction] = useState<StashAction | null>(null);
	const [dropTarget, setDropTarget] = useState<RepositoryStashItem | null>(
		null,
	);
	const [includeUntracked, setIncludeUntracked] = useState(true);
	const [isLoading, setIsLoading] = useState(false);
	const [loadError, setLoadError] = useState<string | null>(null);
	const [message, setMessage] = useState("");
	const [open, setOpen] = useState(false);
	const [restoreIndex, setRestoreIndex] = useState(true);
	const [stashes, setStashes] = useState<RepositoryStashItem[]>([]);

	const loadStashes = useCallback(async () => {
		setIsLoading(true);
		setLoadError(null);
		try {
			const response = await sendRequestWithResponse({
				arguments: { knownRepositoryId: repositoryId },
				commandType: NativeMessageType.GetRepositoryRefs,
			});
			setStashes(response.stashes);
		} catch (error) {
			setLoadError(
				error instanceof Error ? error.message : "Failed to load stashes.",
			);
		} finally {
			setIsLoading(false);
		}
	}, [repositoryId]);

	useEffect(() => {
		if (open) {
			void loadStashes();
		}
	}, [open, loadStashes]);

	const runAction = async (
		action: StashAction,
		stash?: RepositoryStashItem,
	) => {
		if (busyAction) {
			return;
		}

		setBusyAction(action);
		const actionLabel = stashActionLabel(action);
		const toastId = toast.loading(`${actionLabel} in progress`);
		try {
			await sendRequestWithResponse(
				{
					arguments: {
						action,
						includeUntracked,
						message:
							action === StashAction.Create ? message.trim() || null : null,
						repositoryId,
						restoreIndex,
						selector: stash?.selector ?? null,
					},
					commandType: NativeMessageType.ManageStash,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			if (action === StashAction.Create) {
				setMessage("");
			}
			setDropTarget(null);
			await Promise.all([loadStashes(), onRepositoryChanged()]);
			toast.success(`${actionLabel} complete`, { id: toastId });
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : `${actionLabel} failed.`,
				{ id: toastId },
			);
		} finally {
			setBusyAction(null);
		}
	};

	return (
		<>
			<Dialog open={open} onOpenChange={setOpen}>
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
								disabled={!canCreate || busyAction !== null}
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
						{isLoading ? <StashLoading /> : null}
						{!isLoading && loadError ? (
							<p className="rounded-md border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">
								{loadError}
							</p>
						) : null}
						{!isLoading && !loadError && stashes.length === 0 ? (
							<StashEmpty />
						) : null}
						{!isLoading && !loadError && stashes.length > 0 ? (
							<div className="grid gap-2">
								{stashes.map((stash) => (
									<StashRow
										busyAction={busyAction}
										key={`${stash.selector}:${stash.commitHash}`}
										onApply={() => void runAction(StashAction.Apply, stash)}
										onDrop={() => setDropTarget(stash)}
										onPop={() => void runAction(StashAction.Pop, stash)}
										stash={stash}
									/>
								))}
							</div>
						) : null}
					</section>
				</DialogContent>
			</Dialog>
			<AlertDialog
				onOpenChange={(isOpen) => {
					if (!isOpen && busyAction !== StashAction.Drop) {
						setDropTarget(null);
					}
				}}
				open={dropTarget !== null}
			>
				<AlertDialogContent>
					<AlertDialogHeader>
						<AlertDialogMedia className="bg-destructive/10 text-destructive">
							<Trash2 aria-hidden="true" />
						</AlertDialogMedia>
						<AlertDialogTitle>Delete this stash?</AlertDialogTitle>
						<AlertDialogDescription>
							{dropTarget?.selector} will be permanently removed from the
							repository reflog.
						</AlertDialogDescription>
					</AlertDialogHeader>
					<AlertDialogFooter>
						<AlertDialogCancel disabled={busyAction === StashAction.Drop}>
							Cancel
						</AlertDialogCancel>
						<AlertDialogAction
							disabled={!dropTarget || busyAction === StashAction.Drop}
							onClick={() => {
								if (dropTarget) void runAction(StashAction.Drop, dropTarget);
							}}
							variant="destructive"
						>
							Delete stash
						</AlertDialogAction>
					</AlertDialogFooter>
				</AlertDialogContent>
			</AlertDialog>
		</>
	);
}

function StashRow({
	busyAction,
	onApply,
	onDrop,
	onPop,
	stash,
}: {
	busyAction: StashAction | null;
	onApply: () => void;
	onDrop: () => void;
	onPop: () => void;
	stash: RepositoryStashItem;
}) {
	const isBusy = busyAction !== null;
	return (
		<article className="grid gap-2 rounded-lg border bg-card p-3">
			<div className="flex min-w-0 items-start gap-3">
				<div className="mt-0.5 rounded-md bg-muted p-1.5 text-muted-foreground">
					<PackageOpen aria-hidden="true" className="size-4" />
				</div>
				<div className="min-w-0 flex-1">
					<div className="flex items-center gap-2">
						<span className="font-mono text-xs text-primary">
							{stash.selector}
						</span>
						<span className="font-mono text-[10px] text-muted-foreground">
							{stash.commitHash.slice(0, 7)}
						</span>
					</div>
					<p className="mt-1 break-words text-sm">
						{stash.message || "Stashed working changes"}
					</p>
					{stash.createdAtUnixSeconds ? (
						<time className="mt-1 block text-xs text-muted-foreground">
							{formatStashDate(stash.createdAtUnixSeconds)}
						</time>
					) : null}
				</div>
			</div>
			<div className="flex justify-end gap-1">
				<Button disabled={isBusy} onClick={onApply} size="sm" variant="ghost">
					<ArchiveRestore aria-hidden="true" />
					Apply
				</Button>
				<Button disabled={isBusy} onClick={onPop} size="sm" variant="ghost">
					<PackageOpen aria-hidden="true" />
					Pop
				</Button>
				<Button
					aria-label={`Delete ${stash.selector}`}
					disabled={isBusy}
					onClick={onDrop}
					size="icon-sm"
					title={`Delete ${stash.selector}`}
					variant="ghost"
				>
					<Trash2 aria-hidden="true" />
				</Button>
			</div>
		</article>
	);
}

function StashLoading() {
	return (
		<div aria-label="Loading stashes" className="grid gap-2" role="status">
			<div className="h-24 animate-pulse rounded-lg bg-muted" />
			<div className="h-24 animate-pulse rounded-lg bg-muted" />
		</div>
	);
}

function StashEmpty() {
	return (
		<div className="grid place-items-center rounded-lg border border-dashed px-4 py-8 text-center">
			<Archive
				aria-hidden="true"
				className="mb-2 size-6 text-muted-foreground"
			/>
			<p className="font-medium">No saved stashes</p>
			<p className="mt-1 max-w-64 text-xs text-muted-foreground">
				Create one above when you need to change context without committing.
			</p>
		</div>
	);
}

function stashActionLabel(action: StashAction) {
	return action === StashAction.Create
		? "Stash changes"
		: action === StashAction.Apply
			? "Apply stash"
			: action === StashAction.Pop
				? "Pop stash"
				: "Delete stash";
}

function formatStashDate(value: number) {
	const date = new Date(value * 1000);
	return Number.isNaN(date.getTime())
		? "Unknown date"
		: new Intl.DateTimeFormat(undefined, {
				dateStyle: "medium",
				timeStyle: "short",
			}).format(date);
}
