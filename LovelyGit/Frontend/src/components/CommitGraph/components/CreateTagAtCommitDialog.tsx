import { Tag } from "lucide-react";
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
import { shortHash } from "../utils/format";

export function CreateTagAtCommitDialog({
	commitHash,
	isOpen,
	onOpenChange,
	onSuccess,
	repositoryId,
}: {
	commitHash: string;
	isOpen: boolean;
	onOpenChange: (isOpen: boolean) => void;
	onSuccess: () => void;
	repositoryId: string | null;
}) {
	const tagNameId = useId();
	const [tagName, setTagName] = useState("");
	const [isCreating, setIsCreating] = useState(false);
	const trimmedTagName = tagName.trim();
	const canCreate =
		!isCreating && repositoryId !== null && trimmedTagName.length > 0;

	useEffect(() => {
		if (isOpen) {
			setTagName("");
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
					commitHash,
					repositoryId,
					tagName: trimmedTagName,
				},
				commandType: NativeMessageType.CreateTagAtCommit,
			});
			toast.success(`Created tag ${trimmedTagName}`);
			onSuccess();
			onOpenChange(false);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not create tag",
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
							<Tag aria-hidden="true" />
							Create tag
						</DialogTitle>
						<DialogDescription>
							Add a lightweight tag at commit {shortHash(commitHash)}.
						</DialogDescription>
					</DialogHeader>
					<div className="grid gap-2">
						<label className="text-sm font-medium" htmlFor={tagNameId}>
							Tag name
						</label>
						<Input
							autoFocus
							disabled={isCreating}
							id={tagNameId}
							onChange={(event) => setTagName(event.currentTarget.value)}
							onInput={(event) => setTagName(event.currentTarget.value)}
							placeholder="v1.0.0"
							value={tagName}
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
							{isCreating ? "Creating" : "Create tag"}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}
