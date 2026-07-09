import { Check, Download, FolderOpen, LoaderCircle, X } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
	DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import type { CloneRepositoryProgressNotification } from "@/generated/types";
import {
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { useRepositoryContext } from "@/lib/repositoryContext";
import { inferCloneDirectoryName } from "./CloneRepositoryHelpers";

const cloneTimeoutMs = 12 * 60 * 60 * 1000;

type CloneStatus = "idle" | "cloning" | "canceling";

export function CloneRepositoryDialog() {
	const repositories = useRepositoryContext();
	const cancelRequestedRef = useRef(false);
	const operationIdRef = useRef<string | null>(null);
	const [directoryName, setDirectoryName] = useState("");
	const [directoryNameEdited, setDirectoryNameEdited] = useState(false);
	const [open, setOpen] = useState(false);
	const [parentPath, setParentPath] = useState("");
	const [progress, setProgress] =
		useState<CloneRepositoryProgressNotification | null>(null);
	const [recurseSubmodules, setRecurseSubmodules] = useState(false);
	const [remoteUrl, setRemoteUrl] = useState("");
	const [shallow, setShallow] = useState(false);
	const [status, setStatus] = useState<CloneStatus>("idle");
	const isBusy = status !== "idle";
	const canClone =
		remoteUrl.trim().length > 0 &&
		parentPath.trim().length > 0 &&
		directoryName.trim().length > 0 &&
		!isBusy;

	useEffect(() => {
		return subscribeToServerEvent("CloneRepositoryProgress", (notification) => {
			if (notification.operationId === operationIdRef.current) {
				setProgress(notification);
			}
		});
	}, []);

	const updateRemoteUrl = (value: string) => {
		setRemoteUrl(value);
		if (!directoryNameEdited) {
			setDirectoryName(inferCloneDirectoryName(value));
		}
	};

	const chooseDestination = async () => {
		try {
			const result = await sendRequestWithResponse({
				commandType: NativeMessageType.ChooseCloneDestination,
			});
			if (result?.parentPath) {
				setParentPath(result.parentPath);
			}
		} catch (error) {
			toast.error(
				error instanceof Error
					? error.message
					: "Could not choose a destination folder.",
			);
		}
	};

	const cloneRepository = async () => {
		if (!canClone) {
			return;
		}

		const operationId = crypto.randomUUID();
		operationIdRef.current = operationId;
		cancelRequestedRef.current = false;
		setProgress({
			message: "Preparing destination",
			operationId,
			phasePercent: null,
			percent: null,
			stage: "Preparing",
		});
		setStatus("cloning");
		try {
			const repository = await sendRequestWithResponse(
				{
					arguments: {
						directoryName: directoryName.trim(),
						operationId,
						parentPath: parentPath.trim(),
						recurseSubmodules,
						remoteUrl: remoteUrl.trim(),
						shallow,
					},
					commandType: NativeMessageType.CloneRepository,
				},
				{ timeoutMs: cloneTimeoutMs },
			);
			await repositories.reloadRepositories();
			await repositories.setCurrentRepositoryId(repository.id);
			setOpen(false);
			toast.success(`Cloned ${repository.name || directoryName.trim()}`);
			resetForm();
		} catch (error) {
			if (cancelRequestedRef.current) {
				toast.info("Clone canceled");
			} else {
				toast.error(
					error instanceof Error ? error.message : "Repository clone failed.",
				);
			}
		} finally {
			operationIdRef.current = null;
			setStatus("idle");
		}
	};

	const cancelClone = async () => {
		const operationId = operationIdRef.current;
		if (!operationId || status !== "cloning") {
			return;
		}

		cancelRequestedRef.current = true;
		setStatus("canceling");
		setProgress((current) =>
			current
				? { ...current, message: "Stopping clone…", stage: "Canceling" }
				: current,
		);
		try {
			await sendRequestWithResponse({
				arguments: { operationId },
				commandType: NativeMessageType.CancelCloneRepository,
			});
		} catch (error) {
			cancelRequestedRef.current = false;
			setStatus("cloning");
			toast.error(
				error instanceof Error ? error.message : "Could not cancel the clone.",
			);
		}
	};

	const resetForm = () => {
		setRemoteUrl("");
		setParentPath("");
		setDirectoryName("");
		setDirectoryNameEdited(false);
		setShallow(false);
		setRecurseSubmodules(false);
		setProgress(null);
	};

	return (
		<Dialog
			onOpenChange={(nextOpen) => {
				if (!nextOpen && isBusy) {
					return;
				}
				setOpen(nextOpen);
			}}
			open={open}
		>
			<DialogTrigger render={<Button size="sm" />}>
				<Download aria-hidden="true" />
				Clone Repository
			</DialogTrigger>
			<DialogContent
				className="gap-0 overflow-hidden p-0 sm:max-w-lg"
				showCloseButton={!isBusy}
			>
				<DialogHeader className="border-b px-5 py-4">
					<DialogTitle>Clone repository</DialogTitle>
					<DialogDescription>
						Download a remote repository and open it in LovelyGit.
					</DialogDescription>
				</DialogHeader>
				<div className="grid gap-4 px-5 py-4">
					<label className="grid gap-2 text-sm" htmlFor="clone-remote-url">
						<span className="font-medium">Repository URL</span>
						<Input
							aria-label="Repository URL"
							autoFocus
							disabled={isBusy}
							id="clone-remote-url"
							onChange={(event) => updateRemoteUrl(event.currentTarget.value)}
							onInput={(event) => updateRemoteUrl(event.currentTarget.value)}
							placeholder="https://github.com/owner/repository.git"
							spellCheck={false}
							value={remoteUrl}
						/>
					</label>
					<div className="grid gap-2 text-sm">
						<label className="font-medium" htmlFor="clone-parent-path">
							Destination folder
						</label>
						<div className="flex gap-2">
							<Input
								aria-label="Destination folder"
								disabled={isBusy}
								id="clone-parent-path"
								onChange={(event) => setParentPath(event.currentTarget.value)}
								onInput={(event) => setParentPath(event.currentTarget.value)}
								placeholder="Choose or enter a folder"
								spellCheck={false}
								value={parentPath}
							/>
							<Button
								aria-label="Browse for destination folder"
								disabled={isBusy}
								onClick={() => void chooseDestination()}
								size="icon"
								title="Browse for destination folder"
								type="button"
								variant="outline"
							>
								<FolderOpen aria-hidden="true" />
							</Button>
						</div>
					</div>
					<label className="grid gap-2 text-sm" htmlFor="clone-directory-name">
						<span className="font-medium">Repository folder name</span>
						<Input
							aria-label="Repository folder name"
							disabled={isBusy}
							id="clone-directory-name"
							onChange={(event) => {
								setDirectoryNameEdited(true);
								setDirectoryName(event.currentTarget.value);
							}}
							onInput={(event) => {
								setDirectoryNameEdited(true);
								setDirectoryName(event.currentTarget.value);
							}}
							placeholder="repository"
							spellCheck={false}
							value={directoryName}
						/>
					</label>
					<div className="flex flex-wrap gap-x-5 gap-y-2 rounded-lg border bg-card px-3 py-2.5">
						<label
							className="flex items-center gap-2 text-sm"
							htmlFor="clone-shallow"
						>
							<Checkbox
								aria-label="Shallow clone"
								checked={shallow}
								disabled={isBusy}
								id="clone-shallow"
								onCheckedChange={setShallow}
							/>
							Shallow clone
						</label>
						<label
							className="flex items-center gap-2 text-sm"
							htmlFor="clone-submodules"
						>
							<Checkbox
								aria-label="Initialize submodules"
								checked={recurseSubmodules}
								disabled={isBusy}
								id="clone-submodules"
								onCheckedChange={setRecurseSubmodules}
							/>
							Initialize submodules
						</label>
					</div>
					{isBusy && progress ? <CloneProgress progress={progress} /> : null}
				</div>
				<DialogFooter className="mx-0 mb-0 px-5 pb-4">
					{isBusy ? (
						<Button
							disabled={status === "canceling"}
							onClick={() => void cancelClone()}
							type="button"
							variant="outline"
						>
							{status === "canceling" ? (
								<LoaderCircle aria-hidden="true" className="animate-spin" />
							) : (
								<X aria-hidden="true" />
							)}
							{status === "canceling" ? "Canceling" : "Cancel clone"}
						</Button>
					) : (
						<Button
							disabled={!canClone}
							onClick={() => void cloneRepository()}
							type="button"
						>
							<Download aria-hidden="true" />
							Clone and open
						</Button>
					)}
				</DialogFooter>
			</DialogContent>
		</Dialog>
	);
}

function CloneProgress({
	progress,
}: {
	progress: CloneRepositoryProgressNotification;
}) {
	return (
		<div className="grid gap-2 rounded-lg border bg-muted/40 p-3" role="status">
			<div className="flex items-center gap-2">
				{progress.percent === 100 ? (
					<Check aria-hidden="true" className="size-4 text-primary" />
				) : (
					<LoaderCircle
						aria-hidden="true"
						className="size-4 animate-spin text-primary"
					/>
				)}
				<span className="min-w-0 flex-1 truncate font-medium text-sm">
					{progress.stage}
				</span>
			</div>
			<CloneProgressBar label="Overall progress" percent={progress.percent} />
			<CloneProgressBar
				label={`Current phase: ${progress.stage}`}
				percent={progress.phasePercent}
			/>
			<p
				className="truncate text-muted-foreground text-xs"
				title={progress.message}
			>
				{progress.message}
			</p>
		</div>
	);
}

function CloneProgressBar({
	label,
	percent,
}: {
	label: string;
	percent: number | null;
}) {
	return (
		<div className="grid gap-1">
			<div className="flex items-center gap-2 text-muted-foreground text-xs">
				<span className="min-w-0 flex-1 truncate">{label}</span>
				{percent != null ? (
					<span className="shrink-0 font-mono">{percent}%</span>
				) : (
					<span className="shrink-0">Waiting for Git…</span>
				)}
			</div>
			<div
				aria-label={label}
				aria-valuemax={100}
				aria-valuemin={0}
				aria-valuenow={percent ?? undefined}
				className="h-1.5 overflow-hidden rounded-full bg-muted"
				role="progressbar"
			>
				<div
					className={
						percent == null
							? "h-full w-1/3 animate-pulse rounded-full bg-primary"
							: "h-full rounded-full bg-primary transition-[width] duration-150"
					}
					style={percent == null ? undefined : { width: `${percent}%` }}
				/>
			</div>
		</div>
	);
}
