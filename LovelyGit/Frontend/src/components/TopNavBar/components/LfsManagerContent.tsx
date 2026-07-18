import { useEffect, useState } from "react";
import {
	Download,
	HardDriveDownload,
	LoaderCircle,
	Plus,
	RefreshCw,
	Sparkles,
	Trash2,
} from "@/components/icons/lovelyIcons";
import {
	AlertDialog,
	AlertDialogAction,
	AlertDialogCancel,
	AlertDialogContent,
	AlertDialogDescription,
	AlertDialogFooter,
	AlertDialogHeader,
	AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import {
	DialogContent,
	DialogDescription,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { LfsPatternVirtualList } from "./LfsPatternVirtualList";
import { useLfsManager } from "./useLfsManager";

export function LfsManagerContent({ repositoryId }: { repositoryId: string }) {
	const manager = useLfsManager(repositoryId);
	const [pattern, setPattern] = useState("");
	const [confirmPrune, setConfirmPrune] = useState(false);
	// This content mounts once per dialog opening; depending on load would loop.
	// biome-ignore lint/correctness/useExhaustiveDependencies: mount is the open event
	useEffect(() => void manager.load(), []);
	const busy = manager.busyAction !== null;
	return (
		<>
			<DialogContent className="grid max-h-[84vh] grid-rows-[auto_minmax(0,1fr)] overflow-hidden sm:max-w-2xl">
				<DialogHeader>
					<DialogTitle className="flex items-center gap-2">
						<HardDriveDownload className="size-5 text-primary" /> Git LFS
					</DialogTitle>
					<DialogDescription>
						Track large assets outside normal Git history and manage the local
						LFS object cache.
					</DialogDescription>
				</DialogHeader>
				{manager.isLoading ? <Loading /> : null}
				{manager.error ? (
					<div className="rounded-lg border border-destructive/40 bg-destructive/5 p-3 text-destructive text-sm">
						{manager.error}
					</div>
				) : null}
				{manager.state ? (
					<div className="flex min-h-0 flex-col gap-4 overflow-hidden">
						<StatusCard
							available={manager.state.isAvailable}
							busy={busy}
							initialized={manager.state.isInitialized}
							onInstall={() => void manager.run("Install")}
						/>
						<div className="grid grid-cols-3 gap-2">
							<ActionButton
								icon={Download}
								label="Fetch objects"
								onClick={() => void manager.run("Fetch")}
								disabled={busy || !manager.state.isAvailable}
							/>
							<ActionButton
								icon={RefreshCw}
								label="Pull objects"
								onClick={() => void manager.run("Pull")}
								disabled={busy || !manager.state.isAvailable}
							/>
							<ActionButton
								icon={Trash2}
								label="Prune cache"
								onClick={() => setConfirmPrune(true)}
								disabled={busy || !manager.state.isAvailable}
							/>
						</div>
						<form
							className="flex gap-2"
							onSubmit={async (event) => {
								event.preventDefault();
								if (await manager.run("Track", pattern.trim())) setPattern("");
							}}
						>
							<Input
								aria-label="LFS path pattern"
								disabled={busy || !manager.state.isAvailable}
								onChange={(event) => setPattern(event.target.value)}
								placeholder="e.g. *.psd or Assets/**"
								value={pattern}
							/>
							<Button
								disabled={busy || !manager.state.isAvailable || !pattern.trim()}
								type="submit"
							>
								<Plus /> Track pattern
							</Button>
						</form>
						<div className="flex items-center justify-between text-xs">
							<span className="font-medium">Tracked patterns</span>
							<span className="text-muted-foreground">
								{manager.state.trackedPatterns.length} configured
							</span>
						</div>
						<div className="min-h-20 flex-1 overflow-hidden">
							{manager.state.trackedPatterns.length > 0 ? (
								<LfsPatternVirtualList
									busyPattern={
										manager.busyAction === "Untrack"
											? manager.busyPattern
											: null
									}
									disabled={busy}
									onRemove={(item) => void manager.run("Untrack", item)}
									patterns={manager.state.trackedPatterns}
								/>
							) : null}
							{manager.state.trackedPatterns.length === 0 ? (
								<p className="py-5 text-center text-muted-foreground text-sm">
									No path patterns are tracked by Git LFS yet.
								</p>
							) : null}
						</div>
					</div>
				) : null}
			</DialogContent>
			<AlertDialog open={confirmPrune} onOpenChange={setConfirmPrune}>
				<AlertDialogContent>
					<AlertDialogHeader>
						<AlertDialogTitle>Prune the local LFS cache?</AlertDialogTitle>
						<AlertDialogDescription>
							Git LFS will remove old local objects that it considers safe to
							delete. Objects needed by the current checkout remain available.
						</AlertDialogDescription>
					</AlertDialogHeader>
					<AlertDialogFooter>
						<AlertDialogCancel>Cancel</AlertDialogCancel>
						<AlertDialogAction onClick={() => void manager.run("Prune")}>
							Prune cache
						</AlertDialogAction>
					</AlertDialogFooter>
				</AlertDialogContent>
			</AlertDialog>
		</>
	);
}

function StatusCard({
	available,
	busy,
	initialized,
	onInstall,
}: {
	available: boolean;
	busy: boolean;
	initialized: boolean;
	onInstall: () => void;
}) {
	return (
		<div className="flex items-center gap-3 rounded-lg border bg-card p-3">
			<div className="grid size-9 place-items-center rounded-full bg-primary/10 text-primary">
				<Sparkles className="size-4" />
			</div>
			<div className="min-w-0 flex-1">
				<p className="font-medium text-sm">
					{statusTitle(available, initialized)}
				</p>
				<p className="text-muted-foreground text-xs">
					{statusDescription(available, initialized)}
				</p>
			</div>
			<Button
				disabled={busy || !available}
				onClick={onInstall}
				size="sm"
				variant="outline"
			>
				{available && initialized ? "Reinstall hooks" : "Initialize"}
			</Button>
		</div>
	);
}

function statusTitle(available: boolean, initialized: boolean) {
	if (!available) return "Git LFS is unavailable";
	return initialized ? "Git LFS is ready" : "Git LFS needs initialization";
}

function statusDescription(available: boolean, initialized: boolean) {
	if (!available)
		return "Install Git LFS or use the Git distribution bundled with LovelyGit.";
	return initialized
		? "The repository-local pre-push hook is installed."
		: "Initialize repository-local hooks without changing global Git settings.";
}

function ActionButton({
	disabled,
	icon: Icon,
	label,
	onClick,
}: {
	disabled: boolean;
	icon: typeof Download;
	label: string;
	onClick: () => void;
}) {
	return (
		<Button
			className="min-w-0"
			disabled={disabled}
			onClick={onClick}
			variant="outline"
		>
			<Icon /> <span className="truncate">{label}</span>
		</Button>
	);
}

function Loading() {
	return (
		<div className="flex items-center justify-center gap-2 py-10 text-muted-foreground text-sm">
			<LoaderCircle className="animate-spin" /> Reading Git LFS state…
		</div>
	);
}
