import { GitBranch } from "lucide-react";
import { type FormEvent, useEffect, useId, useState } from "react";
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
import { NativeMessageType } from "@/lib/nativeMessaging";

export function RenameBranchDialog({
	branchName,
	isOpen,
	onOpenChange,
	onSuccess,
	repositoryId,
}: {
	branchName: string;
	isOpen: boolean;
	onOpenChange: (isOpen: boolean) => void;
	onSuccess: () => void;
	repositoryId: string | null;
}) {
	const branchNameId = useId();
	const [newBranchName, setNewBranchName] = useState(branchName);
	const [isRenaming, setIsRenaming] = useState(false);
	const trimmedBranchName = newBranchName.trim();
	const canRename =
		!isRenaming &&
		repositoryId !== null &&
		trimmedBranchName.length > 0 &&
		trimmedBranchName !== branchName;

	useEffect(() => {
		if (isOpen) {
			setNewBranchName(branchName);
		}
	}, [branchName, isOpen]);

	const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
		event.preventDefault();
		if (!canRename || repositoryId === null) {
			return;
		}

		setIsRenaming(true);
		try {
			await sendRequestWithResponse({
				arguments: {
					newBranchName: trimmedBranchName,
					oldBranchName: branchName,
					repositoryId,
				},
				commandType: NativeMessageType.RenameBranch,
			});
			toast.success(`Renamed branch to ${trimmedBranchName}`);
			onSuccess();
			onOpenChange(false);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not rename branch",
			);
		} finally {
			setIsRenaming(false);
		}
	};

	return (
		<Dialog onOpenChange={onOpenChange} open={isOpen}>
			<DialogContent>
				<form className="grid gap-4" onSubmit={handleSubmit}>
					<DialogHeader>
						<DialogTitle className="flex items-center gap-2">
							<GitBranch aria-hidden="true" />
							Rename branch
						</DialogTitle>
						<DialogDescription>
							Rename local branch {branchName}.
						</DialogDescription>
					</DialogHeader>
					<div className="grid gap-2">
						<label className="text-sm font-medium" htmlFor={branchNameId}>
							New branch name
						</label>
						<Input
							autoFocus
							disabled={isRenaming}
							id={branchNameId}
							onChange={(event) => setNewBranchName(event.currentTarget.value)}
							onInput={(event) => setNewBranchName(event.currentTarget.value)}
							value={newBranchName}
						/>
					</div>
					<DialogFooter>
						<Button
							disabled={isRenaming}
							onClick={() => onOpenChange(false)}
							type="button"
							variant="outline"
						>
							Cancel
						</Button>
						<Button disabled={!canRename} type="submit">
							{isRenaming ? "Renaming" : "Rename branch"}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}
