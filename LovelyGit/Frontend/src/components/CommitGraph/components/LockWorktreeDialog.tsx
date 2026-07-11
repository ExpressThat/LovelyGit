import { useState } from "react";
import { LoaderCircle, LockKeyhole } from "@/components/icons/lovelyIcons";
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
import type { RepositoryWorktreeItem } from "@/generated/types";

export function LockWorktreeDialog({
	isBusy,
	onClose,
	onConfirm,
	worktree,
}: {
	isBusy: boolean;
	onClose: () => void;
	onConfirm: (reason: string) => void;
	worktree: RepositoryWorktreeItem;
}) {
	const [reason, setReason] = useState("");
	return (
		<Dialog open onOpenChange={(open) => !open && !isBusy && onClose()}>
			<DialogContent>
				<form
					onSubmit={(event) => {
						event.preventDefault();
						onConfirm(reason);
					}}
				>
					<DialogHeader>
						<DialogTitle className="flex items-center gap-2">
							<LockKeyhole
								aria-hidden="true"
								className="size-5 text-amber-500"
							/>
							Lock {worktree.branchName ?? "worktree"}
						</DialogTitle>
						<DialogDescription>
							Prevent Git from pruning or moving this linked worktree while it
							is temporarily unavailable.
						</DialogDescription>
					</DialogHeader>
					<label
						className="grid gap-2 py-4 text-sm"
						htmlFor="worktree-lock-reason"
					>
						<span className="font-medium">Reason (optional)</span>
						<Input
							autoFocus
							disabled={isBusy}
							id="worktree-lock-reason"
							onChange={(event) => setReason(event.currentTarget.value)}
							onInput={(event) => setReason(event.currentTarget.value)}
							placeholder="For example: external drive disconnected"
							value={reason}
						/>
					</label>
					<DialogFooter>
						<Button
							disabled={isBusy}
							onClick={onClose}
							type="button"
							variant="outline"
						>
							Cancel
						</Button>
						<Button disabled={isBusy} type="submit">
							{isBusy ? (
								<LoaderCircle className="animate-spin" />
							) : (
								<LockKeyhole />
							)}
							{isBusy ? "Locking" : "Lock worktree"}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}
