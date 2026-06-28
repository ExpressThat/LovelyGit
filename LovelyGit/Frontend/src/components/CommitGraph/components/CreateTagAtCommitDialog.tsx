import { Tag } from "lucide-react";
import {
	type FormEvent,
	type MouseEvent,
	useEffect,
	useId,
	useState,
} from "react";
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
	const annotatedTagId = useId();
	const tagMessageId = useId();
	const [tagName, setTagName] = useState("");
	const [isAnnotated, setIsAnnotated] = useState(false);
	const [message, setMessage] = useState("");
	const [isCreating, setIsCreating] = useState(false);
	const trimmedTagName = tagName.trim();
	const trimmedMessage = message.trim();
	const canCreate =
		!isCreating &&
		repositoryId !== null &&
		trimmedTagName.length > 0 &&
		(!isAnnotated || trimmedMessage.length > 0);

	useEffect(() => {
		if (isOpen) {
			setTagName("");
			setMessage("");
			setIsAnnotated(false);
		}
	}, [isOpen]);

	const createTag = async () => {
		if (!canCreate || repositoryId === null) {
			return;
		}

		setIsCreating(true);
		try {
			await sendRequestWithResponse({
				arguments: {
					commitHash,
					isAnnotated,
					message: isAnnotated ? trimmedMessage : "",
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
	const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
		event.preventDefault();
		void createTag();
	};
	const handleCreateClick = (event: MouseEvent<HTMLButtonElement>) => {
		event.preventDefault();
		void createTag();
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
							Add a tag at commit {shortHash(commitHash)}.
						</DialogDescription>
					</DialogHeader>
					<div className="grid gap-2">
						<label className="text-sm font-medium" htmlFor={tagNameId}>
							Tag name
						</label>
						<Input
							aria-label="Tag name"
							autoComplete="off"
							autoFocus
							disabled={isCreating}
							id={tagNameId}
							onChange={(event) => setTagName(event.currentTarget.value)}
							onInput={(event) => setTagName(event.currentTarget.value)}
							placeholder="v1.0.0"
							value={tagName}
						/>
					</div>
					<div className="flex items-center gap-2">
						<Checkbox
							aria-label="Create annotated tag"
							checked={isAnnotated}
							disabled={isCreating}
							id={annotatedTagId}
							onCheckedChange={(checked) => setIsAnnotated(checked === true)}
						/>
						<button
							aria-controls={`${tagMessageId}-message`}
							aria-expanded={isAnnotated}
							className="text-left text-sm disabled:cursor-not-allowed disabled:opacity-70"
							disabled={isCreating}
							onClick={() => setIsAnnotated((current) => !current)}
							type="button"
						>
							Create annotated tag
						</button>
					</div>
					{isAnnotated ? (
						<div className="grid gap-2">
							<label
								className="text-sm font-medium"
								htmlFor={`${tagMessageId}-message`}
							>
								Tag message
							</label>
							<Input
								aria-label="Tag message"
								autoComplete="off"
								disabled={isCreating}
								id={`${tagMessageId}-message`}
								onChange={(event) => setMessage(event.currentTarget.value)}
								onInput={(event) => setMessage(event.currentTarget.value)}
								placeholder="Release notes or milestone"
								value={message}
							/>
						</div>
					) : null}
					<DialogFooter>
						<Button
							disabled={isCreating}
							onClick={() => onOpenChange(false)}
							type="button"
							variant="outline"
						>
							Cancel
						</Button>
						<Button
							disabled={!canCreate}
							onClick={handleCreateClick}
							type="submit"
						>
							{isCreating ? "Creating" : "Create tag"}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}
