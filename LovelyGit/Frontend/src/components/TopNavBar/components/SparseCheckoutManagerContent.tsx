import { useEffect, useMemo, useRef, useState } from "react";
import { LovelySwitch } from "@/components/controls/LovelySwitch";
import {
	AlertTriangle,
	FolderGit2,
	LoaderCircle,
	RefreshCw,
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
	DialogFooter,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import { useSparseCheckoutManager } from "./useSparseCheckoutManager";

export function SparseCheckoutManagerContent({
	repositoryId,
}: {
	repositoryId: string;
}) {
	const controller = useSparseCheckoutManager(repositoryId);
	const editorRef = useRef<HTMLTextAreaElement>(null);
	const [hasPatterns, setHasPatterns] = useState(false);
	const [coneMode, setConeMode] = useState(true);
	const [confirmDisable, setConfirmDisable] = useState(false);
	const { state } = controller;
	const stateDraft = useMemo(() => state?.patternText ?? "", [state]);

	// This content mounts once per dialog opening; depending on load would loop.
	// biome-ignore lint/correctness/useExhaustiveDependencies: mount is the open event
	useEffect(() => {
		void controller.load();
	}, []);
	useEffect(() => {
		if (!state) return;
		if (editorRef.current) editorRef.current.value = stateDraft;
		setHasPatterns(state.patternCount > 0);
		setConeMode(state.enabled ? state.coneMode : true);
	}, [state, stateDraft]);

	const busy = controller.busyAction !== null;
	return (
		<>
			<DialogContent className="max-w-2xl">
				<DialogHeader>
					<DialogTitle className="flex items-center gap-2">
						<FolderGit2 aria-hidden="true" className="size-5 text-primary" />
						Sparse checkout
					</DialogTitle>
					<DialogDescription>
						Keep only the directories you work on in large repositories. Git
						retains the complete history and object database.
					</DialogDescription>
				</DialogHeader>
				{controller.error ? (
					<div className="flex items-center justify-between gap-3 rounded-lg border border-destructive/35 bg-destructive/10 p-3 text-destructive text-sm">
						<span>{controller.error}</span>
						<Button
							onClick={() => void controller.load()}
							size="sm"
							variant="outline"
						>
							<RefreshCw aria-hidden="true" /> Retry
						</Button>
					</div>
				) : null}
				<div className="grid gap-4 py-2">
					<div className="rounded-lg border bg-card p-3">
						<p className="font-medium text-sm">
							{state?.enabled
								? "Sparse working tree active"
								: "Full working tree active"}
						</p>
						<p className="mt-1 text-muted-foreground text-xs">
							{state?.enabled
								? `${state.patternCount} selection${state.patternCount === 1 ? "" : "s"} checked out`
								: "Every tracked path is present on disk."}
						</p>
					</div>
					<div className="grid gap-2">
						<label
							className="font-medium text-sm"
							htmlFor="sparse-checkout-patterns"
						>
							{coneMode ? "Directories" : "Git ignore-style patterns"}
						</label>
						<textarea
							className="custom-scrollbar min-h-36 resize-y rounded-md border border-input bg-background px-3 py-2 font-mono text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
							defaultValue={stateDraft}
							disabled={busy || controller.isLoading}
							id="sparse-checkout-patterns"
							onInput={(event) =>
								setHasPatterns(hasNonWhitespace(event.currentTarget.value))
							}
							placeholder={
								coneMode ? "src\ndocs\napps/desktop" : "/*\n!/vendor/"
							}
							ref={editorRef}
						/>
						<p className="text-muted-foreground text-xs">
							One {coneMode ? "repository-relative directory" : "pattern"} per
							line.
						</p>
					</div>
					<div className="flex items-center justify-between gap-4 rounded-lg border bg-muted/35 p-3">
						<div className="min-w-0">
							<label className="font-medium text-sm" htmlFor="sparse-cone-mode">
								Cone mode
							</label>
							<p className="mt-1 text-muted-foreground text-xs">
								Faster directory-based matching, recommended for monorepos.
							</p>
						</div>
						<div className="flex shrink-0 items-center gap-2">
							<span className="w-5 text-right font-medium text-muted-foreground text-xs">
								{coneMode ? "On" : "Off"}
							</span>
							<LovelySwitch
								checked={coneMode}
								disabled={busy || controller.isLoading}
								id="sparse-cone-mode"
								onCheckedChange={setConeMode}
							/>
						</div>
					</div>
				</div>
				<DialogFooter className="justify-between sm:justify-between">
					<Button
						disabled={!state?.enabled || busy}
						onClick={() => setConfirmDisable(true)}
						variant="outline"
					>
						Restore full checkout
					</Button>
					<Button
						disabled={!hasPatterns || busy || controller.isLoading}
						onClick={() => {
							const patternText = editorRef.current?.value ?? "";
							if (hasNonWhitespace(patternText))
								void controller.run("Set", coneMode, patternText);
						}}
					>
						{busy ? (
							<LoaderCircle aria-hidden="true" className="animate-spin" />
						) : null}
						{state?.enabled ? "Apply selection" : "Enable sparse checkout"}
					</Button>
				</DialogFooter>
			</DialogContent>
			<AlertDialog onOpenChange={setConfirmDisable} open={confirmDisable}>
				<AlertDialogContent>
					<AlertDialogHeader>
						<AlertDialogTitle className="flex items-center gap-2">
							<AlertTriangle
								aria-hidden="true"
								className="size-5 text-primary"
							/>
							Restore the full working tree?
						</AlertDialogTitle>
						<AlertDialogDescription>
							Git will materialize every tracked path. Large repositories may
							use significantly more disk space.
						</AlertDialogDescription>
					</AlertDialogHeader>
					<AlertDialogFooter>
						<AlertDialogCancel disabled={busy}>
							Keep sparse checkout
						</AlertDialogCancel>
						<AlertDialogAction
							disabled={busy}
							onClick={() => void controller.run("Disable", false, "")}
						>
							Restore all files
						</AlertDialogAction>
					</AlertDialogFooter>
				</AlertDialogContent>
			</AlertDialog>
		</>
	);
}

function hasNonWhitespace(value: string) {
	for (let index = 0; index < value.length; index++) {
		if (!/\s/.test(value[index] ?? "")) return true;
	}
	return false;
}
