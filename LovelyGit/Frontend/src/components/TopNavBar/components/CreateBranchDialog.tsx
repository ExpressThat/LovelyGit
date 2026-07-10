import { GitBranch, LoaderCircle } from "lucide-react";
import { useEffect, useState } from "react";
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
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { NativeMessageType } from "@/lib/nativeMessaging";

export function CreateBranchDialog({
	currentBranchName,
	onBranchChanged,
	onOpenChange,
	open,
	repositoryId,
}: {
	currentBranchName: string | null;
	onBranchChanged: (branchName: string) => void;
	onOpenChange: (open: boolean) => void;
	open: boolean;
	repositoryId: string | null;
}) {
	const [branchName, setBranchName] = useState("");
	const [isCreating, setIsCreating] = useState(false);

	useEffect(() => {
		if (!open) setBranchName("");
	}, [open]);

	const createBranch = async () => {
		const normalizedName = branchName.trim();
		if (!repositoryId || normalizedName.length === 0 || isCreating) return;

		setIsCreating(true);
		const toastId = toast.loading(`Creating ${normalizedName}`);
		try {
			await sendRequestWithResponse(
				{
					arguments: {
						branchName: normalizedName,
						repositoryId,
						startPoint: currentBranchName ?? "HEAD",
					},
					commandType: NativeMessageType.CreateBranch,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			onOpenChange(false);
			onBranchChanged(normalizedName);
			toast.success(`Created and switched to ${normalizedName}`, {
				id: toastId,
			});
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
		<Dialog open={open} onOpenChange={onOpenChange}>
			<DialogContent>
				<form
					onSubmit={(event) => {
						event.preventDefault();
						void createBranch();
					}}
				>
					<DialogHeader>
						<DialogTitle>Create branch</DialogTitle>
						<DialogDescription>
							Create from {currentBranchName ?? "HEAD"} and switch to it.
						</DialogDescription>
					</DialogHeader>
					<div className="py-4">
						<label className="grid gap-2 text-sm" htmlFor="new-branch-name">
							<span className="font-medium">Branch name</span>
							<Input
								autoFocus
								id="new-branch-name"
								onChange={(event) => setBranchName(event.currentTarget.value)}
								placeholder="feature/my-change"
								value={branchName}
							/>
						</label>
					</div>
					<DialogFooter className="mx-0 mb-0 px-0 pb-0">
						<Button disabled={!branchName.trim() || isCreating} type="submit">
							{isCreating ? (
								<LoaderCircle aria-hidden="true" className="animate-spin" />
							) : (
								<GitBranch aria-hidden="true" />
							)}
							Create and switch
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}
