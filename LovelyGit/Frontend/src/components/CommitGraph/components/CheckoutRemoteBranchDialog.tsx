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

export function CheckoutRemoteBranchDialog({
	isOpen,
	onOpenChange,
	onSuccess,
	remoteBranchName,
	repositoryId,
}: {
	isOpen: boolean;
	onOpenChange: (isOpen: boolean) => void;
	onSuccess: () => void;
	remoteBranchName: string;
	repositoryId: string | null;
}) {
	const localBranchNameId = useId();
	const [localBranchName, setLocalBranchName] = useState(
		defaultLocalBranchName(remoteBranchName),
	);
	const [isCheckingOut, setIsCheckingOut] = useState(false);
	const trimmedLocalBranchName = localBranchName.trim();
	const canCheckout =
		!isCheckingOut &&
		repositoryId !== null &&
		trimmedLocalBranchName.length > 0;

	useEffect(() => {
		if (isOpen) {
			setLocalBranchName(defaultLocalBranchName(remoteBranchName));
		}
	}, [isOpen, remoteBranchName]);

	const checkoutRemoteBranch = async (event: FormEvent<HTMLFormElement>) => {
		event.preventDefault();
		if (!canCheckout || repositoryId === null) {
			return;
		}

		setIsCheckingOut(true);
		try {
			await sendRequestWithResponse({
				arguments: {
					localBranchName: trimmedLocalBranchName,
					remoteBranchName,
					repositoryId,
				},
				commandType: NativeMessageType.CheckoutRemoteBranch,
			});
			toast.success(`Checked out ${trimmedLocalBranchName}`);
			onSuccess();
			onOpenChange(false);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not checkout branch",
			);
		} finally {
			setIsCheckingOut(false);
		}
	};

	return (
		<Dialog onOpenChange={onOpenChange} open={isOpen}>
			<DialogContent>
				<form className="grid gap-4" onSubmit={checkoutRemoteBranch}>
					<DialogHeader>
						<DialogTitle className="flex items-center gap-2">
							<GitBranch aria-hidden="true" />
							Checkout remote branch
						</DialogTitle>
						<DialogDescription>
							Create a local branch that tracks {remoteBranchName}.
						</DialogDescription>
					</DialogHeader>
					<div className="grid gap-2">
						<label className="text-sm font-medium" htmlFor={localBranchNameId}>
							Local branch name
						</label>
						<Input
							autoFocus
							disabled={isCheckingOut}
							id={localBranchNameId}
							onChange={(event) =>
								setLocalBranchName(event.currentTarget.value)
							}
							onInput={(event) => setLocalBranchName(event.currentTarget.value)}
							value={localBranchName}
						/>
					</div>
					<DialogFooter>
						<Button
							disabled={isCheckingOut}
							onClick={() => onOpenChange(false)}
							type="button"
							variant="outline"
						>
							Cancel
						</Button>
						<Button disabled={!canCheckout} type="submit">
							{isCheckingOut ? "Checking out" : "Checkout branch"}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}

function defaultLocalBranchName(remoteBranchName: string) {
	const slashIndex = remoteBranchName.indexOf("/");
	return slashIndex >= 0
		? remoteBranchName.slice(slashIndex + 1)
		: remoteBranchName;
}
