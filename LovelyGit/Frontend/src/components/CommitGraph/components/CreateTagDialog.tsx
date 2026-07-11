import { useState } from "react";
import { toast } from "sonner";
import { CloudUpload, LoaderCircle, Tag } from "@/components/icons/lovelyIcons";
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
import type { CommitGraphRow } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { shortHash } from "../utils/format";
import { CreateTagAnnotationOptions } from "./CreateTagAnnotationOptions";
import { MutationOptionToggle } from "./MutationOptionToggle";

export function CreateTagDialog({
	commit,
	existingTagNames,
	onOpenChange,
	onRepositoryChanged,
	remoteName,
	repositoryId,
}: {
	commit: CommitGraphRow;
	existingTagNames: string[];
	onOpenChange: (commit: CommitGraphRow | null) => void;
	onRepositoryChanged: () => void;
	remoteName: string | null;
	repositoryId: string | null;
}) {
	const [isAnnotated, setIsAnnotated] = useState(false);
	const [isRunning, setIsRunning] = useState(false);
	const [isSigned, setIsSigned] = useState(false);
	const [message, setMessage] = useState("");
	const [pushAfterCreate, setPushAfterCreate] = useState(false);
	const [tagName, setTagName] = useState("");
	const hash = shortHash(commit.commit.hash);
	const normalizedName = tagName.trim();
	const duplicate = existingTagNames.includes(normalizedName);
	const canCreate =
		normalizedName.length > 0 &&
		!duplicate &&
		(!isAnnotated || message.trim().length > 0);

	const createTag = async () => {
		if (!repositoryId || !canCreate || isRunning) return;
		setIsRunning(true);
		const toastId = toast.loading(`Creating tag ${normalizedName}`);
		try {
			await sendRequestWithResponse(
				{
					arguments: {
						commitHash: commit.commit.hash,
						isAnnotated,
						message: isAnnotated ? message.trim() : null,
						repositoryId,
						sign: isSigned,
						tagName: normalizedName,
					},
					commandType: NativeMessageType.CreateTagAtCommit,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			if (pushAfterCreate && remoteName) {
				try {
					await sendRequestWithResponse(
						{
							arguments: { remoteName, repositoryId, tagName: normalizedName },
							commandType: NativeMessageType.PushTag,
						},
						{ timeoutMs: gitMutationTimeoutMs },
					);
				} catch (pushError) {
					toast.warning(
						`Created ${normalizedName} locally, but could not push it`,
						{
							description:
								pushError instanceof Error ? pushError.message : undefined,
							id: toastId,
						},
					);
					onRepositoryChanged();
					onOpenChange(null);
					return;
				}
			}
			onRepositoryChanged();
			onOpenChange(null);
			toast.success(
				pushAfterCreate && remoteName
					? `Created and pushed ${normalizedName}`
					: `Created tag ${normalizedName}`,
				{ id: toastId },
			);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not create tag.",
				{ id: toastId },
			);
		} finally {
			setIsRunning(false);
		}
	};

	return (
		<Dialog
			onOpenChange={(open) => !open && !isRunning && onOpenChange(null)}
			open
		>
			<DialogContent>
				<div>
					<DialogHeader>
						<DialogTitle>Create tag at {hash}</DialogTitle>
						<DialogDescription>
							Mark this exact commit with a durable repository ref.
						</DialogDescription>
					</DialogHeader>
					<div className="grid gap-4 py-4">
						<div className="grid gap-2">
							<label className="font-medium text-sm" htmlFor="tag-name">
								Tag name
							</label>
							<Input
								aria-label="Tag name"
								autoFocus
								id="tag-name"
								onChange={(event) => setTagName(event.currentTarget.value)}
								onInput={(event) => setTagName(event.currentTarget.value)}
								placeholder="v1.0.0"
								value={tagName}
							/>
							{duplicate ? (
								<p className="text-destructive text-xs">
									A local tag with this name already exists.
								</p>
							) : null}
						</div>
						<CreateTagAnnotationOptions
							disabled={isRunning}
							isAnnotated={isAnnotated}
							isSigned={isSigned}
							message={message}
							onAnnotatedChange={(checked) => {
								setIsAnnotated(checked);
								if (!checked) setIsSigned(false);
							}}
							onMessageChange={setMessage}
							onSignedChange={setIsSigned}
						/>
						{remoteName ? (
							<MutationOptionToggle
								accessibleName={`Push to ${remoteName} after creating`}
								checked={pushAfterCreate}
								disabled={isRunning}
								icon={
									<CloudUpload
										aria-hidden="true"
										className="size-4 text-muted-foreground"
									/>
								}
								id="toggle-push-tag"
								onCheckedChange={setPushAfterCreate}
							>
								Push to {remoteName} after creating
							</MutationOptionToggle>
						) : null}
					</div>
					<DialogFooter className="mx-0 mb-0 px-0 pb-0">
						<Button
							disabled={!canCreate || isRunning}
							onClick={() => void createTag()}
							type="button"
						>
							{isRunning ? (
								<LoaderCircle aria-hidden="true" className="animate-spin" />
							) : (
								<Tag aria-hidden="true" />
							)}
							{isRunning ? "Creating tag" : "Create tag"}
						</Button>
					</DialogFooter>
				</div>
			</DialogContent>
		</Dialog>
	);
}
