import { Archive } from "lucide-react";
import { type FormEvent, useEffect, useId, useState } from "react";
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

const gitMutationTimeoutMs = 120_000;

export function StashActionControl({
	onStashCreated,
	repositoryId,
	workingChangesKnown,
	workingChangesCount,
}: {
	onStashCreated: () => void;
	repositoryId: string | null;
	workingChangesKnown: boolean;
	workingChangesCount: number;
}) {
	const messageId = useId();
	const excludeUntrackedId = useId();
	const [isOpen, setIsOpen] = useState(false);
	const [isStashing, setIsStashing] = useState(false);
	const [excludeUntracked, setExcludeUntracked] = useState(false);
	const [message, setMessage] = useState("");
	const trimmedMessage = message.trim();
	const hasKnownNoChanges = workingChangesKnown && workingChangesCount === 0;
	const canOpen = repositoryId !== null && !hasKnownNoChanges;
	const canStash = canOpen && trimmedMessage.length > 0 && !isStashing;

	useEffect(() => {
		if (isOpen) {
			setExcludeUntracked(false);
			setMessage(defaultStashMessage());
		}
	}, [isOpen]);

	const stashChanges = async (event: FormEvent<HTMLFormElement>) => {
		event.preventDefault();
		if (!canStash || repositoryId === null) {
			return;
		}

		setIsStashing(true);
		try {
			await sendRequestWithResponse(
				{
					arguments: {
						includeUntracked: !excludeUntracked,
						message: trimmedMessage,
						repositoryId,
					},
					commandType: NativeMessageType.StashChanges,
				},
				{
					timeoutMs: gitMutationTimeoutMs,
				},
			);
			toast.success(`Created stash ${trimmedMessage}`);
			setIsOpen(false);
			onStashCreated();
		} catch (error) {
			toast.error(error instanceof Error ? error.message : "Could not stash");
		} finally {
			setIsStashing(false);
		}
	};

	return (
		<>
			<Button
				aria-label="Stash working-tree changes"
				className="h-8"
				disabled={!canOpen}
				onClick={() => setIsOpen(true)}
				size="sm"
				title={
					canOpen
						? "Stash working-tree changes"
						: repositoryId !== null && !workingChangesKnown
							? "Checking working-tree changes"
							: "No working-tree changes to stash"
				}
				type="button"
				variant="ghost"
			>
				<Archive aria-hidden="true" />
				<span>Stash</span>
			</Button>
			<Dialog onOpenChange={setIsOpen} open={isOpen}>
				<DialogContent>
					<form className="grid gap-4" onSubmit={stashChanges}>
						<DialogHeader>
							<DialogTitle className="flex items-center gap-2">
								<Archive aria-hidden="true" />
								Stash changes
							</DialogTitle>
							<DialogDescription>
								Save tracked working-tree changes for later. Untracked files can
								be included when needed.
							</DialogDescription>
						</DialogHeader>
						<div className="grid gap-2">
							<label className="text-sm font-medium" htmlFor={messageId}>
								Stash message
							</label>
							<Input
								autoFocus
								disabled={isStashing}
								id={messageId}
								onChange={(event) => setMessage(event.currentTarget.value)}
								onInput={(event) => setMessage(event.currentTarget.value)}
								placeholder="WIP on main"
								value={message}
							/>
						</div>
						<label
							className="flex items-start gap-3 rounded-md border p-3 text-sm"
							htmlFor={excludeUntrackedId}
						>
							<Checkbox
								checked={excludeUntracked}
								disabled={isStashing}
								id={excludeUntrackedId}
								onCheckedChange={(checked) =>
									setExcludeUntracked(checked === true)
								}
							/>
							<span className="grid gap-1">
								<span className="font-medium">Exclude untracked files</span>
								<span className="text-muted-foreground text-xs">
									Keep new files in the working tree and use the faster stash
									path.
								</span>
							</span>
						</label>
						<DialogFooter>
							<Button
								disabled={isStashing}
								onClick={() => setIsOpen(false)}
								type="button"
								variant="outline"
							>
								Cancel
							</Button>
							<Button disabled={!canStash} type="submit">
								{isStashing ? "Stashing" : "Stash changes"}
							</Button>
						</DialogFooter>
					</form>
				</DialogContent>
			</Dialog>
		</>
	);
}

function defaultStashMessage() {
	const now = new Date();
	return `WIP ${now.toLocaleDateString()} ${now.toLocaleTimeString()}`;
}
