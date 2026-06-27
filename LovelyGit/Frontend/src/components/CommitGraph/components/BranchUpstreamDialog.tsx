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

export function BranchUpstreamDialog({
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
	const upstreamId = useId();
	const [upstreamName, setUpstreamName] = useState(`origin/${branchName}`);
	const [isSaving, setIsSaving] = useState(false);
	const trimmedUpstreamName = upstreamName.trim();
	const canSave =
		!isSaving && repositoryId !== null && trimmedUpstreamName.length > 0;

	useEffect(() => {
		if (isOpen) {
			setUpstreamName(`origin/${branchName}`);
		}
	}, [branchName, isOpen]);

	const setUpstream = async (event: FormEvent<HTMLFormElement>) => {
		event.preventDefault();
		if (!canSave || repositoryId === null) {
			return;
		}

		setIsSaving(true);
		try {
			await sendRequestWithResponse({
				arguments: {
					branchName,
					repositoryId,
					upstreamName: trimmedUpstreamName,
				},
				commandType: NativeMessageType.SetBranchUpstream,
			});
			toast.success(`Set upstream to ${trimmedUpstreamName}`);
			onSuccess();
			onOpenChange(false);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not set upstream",
			);
		} finally {
			setIsSaving(false);
		}
	};

	const unsetUpstream = async () => {
		if (isSaving || repositoryId === null) {
			return;
		}

		setIsSaving(true);
		try {
			await sendRequestWithResponse({
				arguments: {
					branchName,
					repositoryId,
				},
				commandType: NativeMessageType.UnsetBranchUpstream,
			});
			toast.success(`Unset upstream for ${branchName}`);
			onSuccess();
			onOpenChange(false);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not unset upstream",
			);
		} finally {
			setIsSaving(false);
		}
	};

	return (
		<Dialog onOpenChange={onOpenChange} open={isOpen}>
			<DialogContent>
				<form className="grid gap-4" onSubmit={setUpstream}>
					<DialogHeader>
						<DialogTitle className="flex items-center gap-2">
							<GitBranch aria-hidden="true" />
							Branch upstream
						</DialogTitle>
						<DialogDescription>
							Set, change, or unset the upstream branch for {branchName}.
						</DialogDescription>
					</DialogHeader>
					<div className="grid gap-2">
						<label className="text-sm font-medium" htmlFor={upstreamId}>
							Upstream branch
						</label>
						<Input
							autoFocus
							disabled={isSaving}
							id={upstreamId}
							onChange={(event) => setUpstreamName(event.currentTarget.value)}
							onInput={(event) => setUpstreamName(event.currentTarget.value)}
							value={upstreamName}
						/>
					</div>
					<DialogFooter>
						<Button
							disabled={isSaving || repositoryId === null}
							onClick={unsetUpstream}
							type="button"
							variant="outline"
						>
							Unset upstream
						</Button>
						<Button disabled={!canSave} type="submit">
							{isSaving ? "Saving" : "Set upstream"}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}
