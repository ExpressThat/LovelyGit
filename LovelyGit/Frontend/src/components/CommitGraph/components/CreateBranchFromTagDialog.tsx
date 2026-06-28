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

export function CreateBranchFromTagDialog({
	isOpen,
	onOpenChange,
	onSuccess,
	repositoryId,
	tagName,
}: {
	isOpen: boolean;
	onOpenChange: (isOpen: boolean) => void;
	onSuccess: () => void;
	repositoryId: string | null;
	tagName: string;
}) {
	const branchNameId = useId();
	const [branchName, setBranchName] = useState("");
	const [isCreating, setIsCreating] = useState(false);
	const trimmedBranchName = branchName.trim();
	const canCreate =
		!isCreating && repositoryId !== null && trimmedBranchName.length > 0;

	useEffect(() => {
		if (isOpen) {
			setBranchName("");
		}
	}, [isOpen]);

	const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
		event.preventDefault();
		if (!canCreate || repositoryId === null) {
			return;
		}

		setIsCreating(true);
		try {
			await sendRequestWithResponse({
				arguments: {
					branchName: trimmedBranchName,
					repositoryId,
					tagName,
				},
				commandType: NativeMessageType.CreateBranchFromTag,
			});
			toast.success(`Created branch ${trimmedBranchName}`);
			onSuccess();
			onOpenChange(false);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not create branch",
			);
		} finally {
			setIsCreating(false);
		}
	};

	return (
		<Dialog onOpenChange={onOpenChange} open={isOpen}>
			<DialogContent>
				<form className="grid gap-4" onSubmit={handleSubmit}>
					<DialogHeader>
						<DialogTitle className="flex items-center gap-2">
							<GitBranch aria-hidden="true" />
							Create branch from tag
						</DialogTitle>
						<DialogDescription>
							Start a new branch at tag {tagName}.
						</DialogDescription>
					</DialogHeader>
					<div className="grid gap-2">
						<label className="text-sm font-medium" htmlFor={branchNameId}>
							Branch name
						</label>
						<Input
							aria-label="Branch name"
							autoComplete="off"
							autoFocus
							disabled={isCreating}
							id={branchNameId}
							onChange={(event) => setBranchName(event.currentTarget.value)}
							onInput={(event) => setBranchName(event.currentTarget.value)}
							placeholder="feature/name"
							value={branchName}
						/>
					</div>
					<DialogFooter>
						<Button
							disabled={isCreating}
							onClick={() => onOpenChange(false)}
							type="button"
							variant="outline"
						>
							Cancel
						</Button>
						<Button disabled={!canCreate} type="submit">
							{isCreating ? "Creating" : "Create branch"}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}
