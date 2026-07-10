import { CloudUpload, LoaderCircle, Tag } from "lucide-react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { useState } from "react";
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
import type { CommitGraphRow } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { shortHash } from "../utils/format";
import { TagOptionToggle } from "./TagOptionToggle";

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
	const [message, setMessage] = useState("");
	const [pushAfterCreate, setPushAfterCreate] = useState(false);
	const [tagName, setTagName] = useState("");
	const reduceMotion = useReducedMotion();
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
								placeholder="v1.0.0"
								value={tagName}
							/>
							{duplicate ? (
								<p className="text-destructive text-xs">
									A local tag with this name already exists.
								</p>
							) : null}
						</div>
						<TagOptionToggle
							accessibleName="Annotated tag with a message"
							checked={isAnnotated}
							id="toggle-annotated-tag"
							onCheckedChange={setIsAnnotated}
						>
							Annotated tag with a message
						</TagOptionToggle>
						<AnimatePresence initial={false}>
							{isAnnotated ? (
								<motion.div
									animate={{ height: "auto", opacity: 1, y: 0 }}
									exit={
										reduceMotion
											? { opacity: 0 }
											: { height: 0, opacity: 0, y: -4 }
									}
									initial={
										reduceMotion
											? { opacity: 0 }
											: { height: 0, opacity: 0, y: -4 }
									}
									className="overflow-hidden"
									transition={{
										duration: reduceMotion ? 0 : 0.2,
										ease: [0.22, 1, 0.36, 1],
									}}
								>
									<label
										className="grid gap-2 font-medium text-sm"
										htmlFor="tag-message"
									>
										Message
										<textarea
											aria-label="Tag message"
											className="min-h-20 resize-y rounded-md border bg-background px-3 py-2 font-normal text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
											id="tag-message"
											onChange={(event) =>
												setMessage(event.currentTarget.value)
											}
											value={message}
										/>
									</label>
								</motion.div>
							) : null}
						</AnimatePresence>
						{remoteName ? (
							<TagOptionToggle
								accessibleName={`Push to ${remoteName} after creating`}
								checked={pushAfterCreate}
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
							</TagOptionToggle>
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
