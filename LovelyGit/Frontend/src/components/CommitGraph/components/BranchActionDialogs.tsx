import { useState } from "react";
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
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
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
import { NativeMessageType } from "@/lib/nativeMessaging";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";

type MutationOptions = {
	onSuccess: () => void;
	repositoryId: string | null;
};

export function useBranchMutation({ onSuccess, repositoryId }: MutationOptions) {
	const [isBusy, setIsBusy] = useState(false);

	const runMutation = async (
		label: string,
		command: Parameters<typeof sendRequestWithResponse>[0],
	) => {
		if (!repositoryId || isBusy) {
			return false;
		}

		setIsBusy(true);
		const toastId = toast.loading(`${label} in progress`);
		try {
			await sendRequestWithResponse(command, {
				timeoutMs: gitMutationTimeoutMs,
			});
			toast.success(`${label} complete`, { id: toastId });
			onSuccess();
			return true;
		} catch (error) {
			toast.error(error instanceof Error ? error.message : `${label} failed`, {
				id: toastId,
			});
			return false;
		} finally {
			setIsBusy(false);
		}
	};

	const runCheckout = ({
		branchName,
		isRemote,
		label,
		localBranchName,
	}: {
		branchName: string;
		isRemote: boolean;
		label: string;
		localBranchName?: string;
	}) =>
		runMutation("Checkout", {
			commandType: NativeMessageType.CheckoutBranch,
			arguments: {
				branchName,
				isRemote,
				localBranchName,
				repositoryId: repositoryId ?? "",
			},
		}).then((success) => {
			if (success) {
				toast.success(`Checked out ${label}`);
			}
			return success;
		});

	return { isBusy, runMutation, runCheckout };
}

export function CreateBranchDialog({
	defaultCheckout = true,
	onOpenChange,
	onSuccess,
	open,
	repositoryId,
	startPoint,
}: {
	defaultCheckout?: boolean;
	onOpenChange: (open: boolean) => void;
	onSuccess: () => void;
	open: boolean;
	repositoryId: string | null;
	startPoint: string;
}) {
	const [branchName, setBranchName] = useState("");
	const [checkout, setCheckout] = useState(defaultCheckout);
	const { isBusy, runMutation } = useBranchMutation({ onSuccess, repositoryId });
	const canSubmit = repositoryId != null && branchName.trim().length > 0 && !isBusy;

	const submit = async () => {
		if (!canSubmit) {
			return;
		}

		const success = await runMutation("Create branch", {
			commandType: NativeMessageType.CreateBranch,
			arguments: {
				branchName: branchName.trim(),
				checkout,
				repositoryId: repositoryId ?? "",
				startPoint,
			},
		});
		if (success) {
			setBranchName("");
			onOpenChange(false);
		}
	};

	return (
		<Dialog open={open} onOpenChange={onOpenChange}>
			<DialogContent>
				<DialogHeader>
					<DialogTitle>Create branch</DialogTitle>
					<DialogDescription>
						Create a local branch at {startPoint === "HEAD" ? "HEAD" : startPoint.slice(0, 8)}.
					</DialogDescription>
				</DialogHeader>
				<div className="grid gap-3">
					<Input
						aria-label="Branch name"
						autoFocus
						onChange={(event) => setBranchName(event.currentTarget.value)}
						onKeyDown={(event) => {
							if (event.key === "Enter") {
								event.preventDefault();
								void submit();
							}
						}}
						placeholder="feature/new-work"
						value={branchName}
					/>
					<label className="flex items-center gap-2 text-sm">
						<Checkbox checked={checkout} onCheckedChange={(checked) => setCheckout(checked === true)} />
						<span>Check out after creating</span>
					</label>
				</div>
				<DialogFooter>
					<Button variant="outline" onClick={() => onOpenChange(false)} type="button">
						Cancel
					</Button>
					<Button disabled={!canSubmit} onClick={() => void submit()} type="button">
						Create
					</Button>
				</DialogFooter>
			</DialogContent>
		</Dialog>
	);
}

export function RenameBranchDialog({
	branchName,
	onOpenChange,
	onSuccess,
	open,
	repositoryId,
}: {
	branchName: string;
	onOpenChange: (open: boolean) => void;
	onSuccess: () => void;
	open: boolean;
	repositoryId: string | null;
}) {
	const [newBranchName, setNewBranchName] = useState(branchName);
	const { isBusy, runMutation } = useBranchMutation({ onSuccess, repositoryId });
	const canSubmit =
		repositoryId != null &&
		newBranchName.trim().length > 0 &&
		newBranchName.trim() !== branchName &&
		!isBusy;

	const submit = async () => {
		if (!canSubmit) {
			return;
		}

		const success = await runMutation("Rename branch", {
			commandType: NativeMessageType.RenameBranch,
			arguments: {
				branchName,
				newBranchName: newBranchName.trim(),
				repositoryId: repositoryId ?? "",
			},
		});
		if (success) {
			onOpenChange(false);
		}
	};

	return (
		<Dialog open={open} onOpenChange={onOpenChange}>
			<DialogContent>
				<DialogHeader>
					<DialogTitle>Rename branch</DialogTitle>
					<DialogDescription>{branchName}</DialogDescription>
				</DialogHeader>
				<Input
					aria-label="New branch name"
					autoFocus
					onChange={(event) => setNewBranchName(event.currentTarget.value)}
					onKeyDown={(event) => {
						if (event.key === "Enter") {
							event.preventDefault();
							void submit();
						}
					}}
					value={newBranchName}
				/>
				<DialogFooter>
					<Button variant="outline" onClick={() => onOpenChange(false)} type="button">
						Cancel
					</Button>
					<Button disabled={!canSubmit} onClick={() => void submit()} type="button">
						Rename
					</Button>
				</DialogFooter>
			</DialogContent>
		</Dialog>
	);
}

export function DeleteBranchDialog({
	branchName,
	onOpenChange,
	onSuccess,
	open,
	repositoryId,
}: {
	branchName: string;
	onOpenChange: (open: boolean) => void;
	onSuccess: () => void;
	open: boolean;
	repositoryId: string | null;
}) {
	const [force, setForce] = useState(false);
	const { isBusy, runMutation } = useBranchMutation({ onSuccess, repositoryId });

	const submit = async () => {
		const success = await runMutation("Delete branch", {
			commandType: NativeMessageType.DeleteBranch,
			arguments: {
				branchName,
				force,
				repositoryId: repositoryId ?? "",
			},
		});
		if (success) {
			setForce(false);
			onOpenChange(false);
		}
	};

	return (
		<AlertDialog open={open} onOpenChange={onOpenChange}>
			<AlertDialogContent>
				<AlertDialogHeader>
					<AlertDialogTitle>Delete branch</AlertDialogTitle>
					<AlertDialogDescription>
						Delete {branchName}. Use force only when Git reports the branch is not fully merged.
					</AlertDialogDescription>
				</AlertDialogHeader>
				<label className="flex items-center gap-2 text-sm">
					<Checkbox checked={force} onCheckedChange={(checked) => setForce(checked === true)} />
					<span>Force delete</span>
				</label>
				<AlertDialogFooter>
					<AlertDialogCancel disabled={isBusy}>Cancel</AlertDialogCancel>
					<AlertDialogAction
						disabled={isBusy || repositoryId == null}
						onClick={() => void submit()}
						variant="destructive"
					>
						Delete
					</AlertDialogAction>
				</AlertDialogFooter>
			</AlertDialogContent>
		</AlertDialog>
	);
}
