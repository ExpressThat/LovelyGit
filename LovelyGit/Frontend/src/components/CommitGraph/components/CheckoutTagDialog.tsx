import { useState } from "react";
import { toast } from "sonner";
import {
	GitCommitHorizontal,
	LoaderCircle,
	Tag,
} from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { motion, useReducedMotion } from "@/lib/motion";
import { NativeMessageType } from "@/lib/nativeMessaging";

export function CheckoutTagDialog({
	onClose,
	onRepositoryChanged,
	repositoryId,
	tagName,
}: {
	onClose: () => void;
	onRepositoryChanged: () => void;
	repositoryId: string | null;
	tagName: string;
}) {
	const [isRunning, setIsRunning] = useState(false);
	const reduceMotion = useReducedMotion();
	const checkout = async () => {
		if (!repositoryId || isRunning) return;
		setIsRunning(true);
		const toastId = toast.loading(`Checking out ${tagName}`);
		try {
			await sendRequestWithResponse(
				{
					arguments: { repositoryId, tagName },
					commandType: NativeMessageType.CheckoutTag,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			onRepositoryChanged();
			onClose();
			toast.success(`Checked out ${tagName} in detached HEAD mode`, {
				id: toastId,
			});
		} catch (error) {
			toast.error(error instanceof Error ? error.message : "Checkout failed.", {
				id: toastId,
			});
		} finally {
			setIsRunning(false);
		}
	};
	return (
		<Dialog onOpenChange={(open) => !open && !isRunning && onClose()} open>
			<DialogContent>
				<DialogHeader>
					<DialogTitle>Checkout tag {tagName}?</DialogTitle>
					<DialogDescription>
						Inspect the tagged snapshot without moving a branch.
					</DialogDescription>
				</DialogHeader>
				<motion.div
					animate={{ opacity: 1, y: 0 }}
					className="my-4 grid gap-3 rounded-lg border bg-card p-4"
					initial={{ opacity: 0, y: reduceMotion ? 0 : 4 }}
				>
					<div className="flex items-center gap-2">
						<Tag aria-hidden="true" className="size-5 text-primary" />
						<code className="truncate font-semibold text-sm">{tagName}</code>
					</div>
					<p className="text-muted-foreground text-xs">
						HEAD becomes detached at this tag. Existing branches remain
						unchanged. Create a branch before committing work you want to keep.
					</p>
				</motion.div>
				<DialogFooter className="mx-0 mb-0 px-0 pb-0">
					<Button
						disabled={!repositoryId || isRunning}
						onClick={() => void checkout()}
						type="button"
					>
						{isRunning ? (
							<LoaderCircle aria-hidden="true" className="animate-spin" />
						) : (
							<GitCommitHorizontal aria-hidden="true" />
						)}
						{isRunning ? "Checking out" : "Checkout detached"}
					</Button>
				</DialogFooter>
			</DialogContent>
		</Dialog>
	);
}
