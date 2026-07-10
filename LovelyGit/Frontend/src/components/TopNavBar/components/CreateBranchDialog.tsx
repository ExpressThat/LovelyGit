import { GitBranch, LoaderCircle } from "lucide-react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { NativeMessageType } from "@/lib/nativeMessaging";

export function CreateBranchDialog({
	currentBranchName,
	existingBranchNames = [],
	onBranchChanged,
	onOpenChange,
	onRepositoryChanged,
	open,
	repositoryId,
	source,
}: {
	currentBranchName: string | null;
	existingBranchNames?: string[];
	onBranchChanged: (branchName: string) => void;
	onOpenChange: (open: boolean) => void;
	onRepositoryChanged: () => void;
	open: boolean;
	repositoryId: string | null;
	source?: BranchCreationSource;
}) {
	const [branchName, setBranchName] = useState("");
	const [checkout, setCheckout] = useState(true);
	const [isCreating, setIsCreating] = useState(false);
	const lastExplicitSource = useRef(source);
	if (source) lastExplicitSource.current = source;
	const displayedSource = source ?? lastExplicitSource.current;
	const reduceMotion = useReducedMotion();
	const normalizedName = branchName.trim();
	const duplicate = existingBranchNames.includes(normalizedName);
	const startPoint = displayedSource?.startPoint ?? currentBranchName ?? "HEAD";
	const sourceLabel = displayedSource?.label ?? currentBranchName ?? "HEAD";
	const canCreate = normalizedName.length > 0 && !duplicate && !isCreating;

	useEffect(() => {
		if (!open) {
			setBranchName("");
			setCheckout(true);
		}
	}, [open]);

	const createBranch = async () => {
		if (!repositoryId || !canCreate) return;

		setIsCreating(true);
		const toastId = toast.loading(`Creating ${normalizedName}`);
		try {
			await sendRequestWithResponse(
				{
					arguments: {
						branchName: normalizedName,
						checkout,
						repositoryId,
						startPoint,
					},
					commandType: NativeMessageType.CreateBranch,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			onOpenChange(false);
			if (checkout) onBranchChanged(normalizedName);
			else onRepositoryChanged();
			toast.success(
				checkout
					? `Created and switched to ${normalizedName}`
					: `Created ${normalizedName}`,
				{
					id: toastId,
				},
			);
		} catch (error) {
			toast.error(
				error instanceof Error
					? error.message
					: `Could not create ${normalizedName}.`,
				{ id: toastId },
			);
		} finally {
			setIsCreating(false);
		}
	};

	return (
		<Dialog
			open={open}
			onOpenChange={(nextOpen) => !isCreating && onOpenChange(nextOpen)}
		>
			<DialogContent>
				<form
					onSubmit={(event) => {
						event.preventDefault();
						void createBranch();
					}}
				>
					<DialogHeader>
						<DialogTitle>Create branch from {sourceLabel}</DialogTitle>
						<DialogDescription>
							Create a local branch at this exact point in history.
						</DialogDescription>
					</DialogHeader>
					<div className="grid gap-3 py-4">
						{displayedSource?.description ? (
							<div className="grid gap-0.5 rounded-lg border bg-card px-3 py-2">
								<span className="truncate font-medium text-sm">
									{displayedSource.description}
								</span>
								<span className="truncate font-mono text-muted-foreground text-xs">
									{displayedSource.startPoint}
								</span>
							</div>
						) : null}
						<label className="grid gap-2 text-sm" htmlFor="new-branch-name">
							<span className="font-medium">Branch name</span>
							<Input
								autoFocus
								id="new-branch-name"
								onChange={(event) => setBranchName(event.currentTarget.value)}
								onInput={(event) => setBranchName(event.currentTarget.value)}
								placeholder="feature/my-change"
								value={branchName}
							/>
						</label>
						<AnimatePresence initial={false}>
							{duplicate ? (
								<motion.p
									animate={{ opacity: 1, y: 0 }}
									exit={{ opacity: 0, y: reduceMotion ? 0 : -3 }}
									initial={{ opacity: 0, y: reduceMotion ? 0 : -3 }}
									className="text-destructive text-xs"
								>
									A local branch with this name already exists.
								</motion.p>
							) : null}
						</AnimatePresence>
						<label
							className="flex cursor-pointer items-center justify-between gap-3 rounded-lg border bg-card px-3 py-2"
							htmlFor="checkout-new-branch"
						>
							<span className="grid gap-0.5">
								<span className="font-medium text-sm">
									Switch to new branch
								</span>
								<span className="text-muted-foreground text-xs">
									Leave this off to create the branch without changing your
									worktree.
								</span>
							</span>
							<Switch
								checked={checkout}
								id="checkout-new-branch"
								onCheckedChange={setCheckout}
							/>
						</label>
					</div>
					<DialogFooter className="mx-0 mb-0 px-0 pb-0">
						<Button disabled={!canCreate} type="submit">
							{isCreating ? (
								<LoaderCircle aria-hidden="true" className="animate-spin" />
							) : (
								<GitBranch aria-hidden="true" />
							)}
							{isCreating
								? "Creating"
								: checkout
									? "Create and switch"
									: "Create branch"}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}

export type BranchCreationSource = {
	description?: string;
	label: string;
	startPoint: string;
};
