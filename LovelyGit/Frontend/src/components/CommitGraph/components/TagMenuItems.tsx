import {
	Copy,
	ExternalLink,
	GitBranch,
	GitCommitHorizontal,
	Trash2,
	Upload,
} from "lucide-react";
import {
	type KeyboardEvent,
	type MouseEvent,
	type PointerEvent,
	useRef,
} from "react";
import { toast } from "sonner";
import { ContextMenuItem } from "@/components/ui/context-menu";
import type { GitRemote } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { copyToClipboard } from "../utils/clipboard";

export function TagMenuItems({
	canCheckoutTag,
	canCreateBranch,
	canDeleteTag,
	canPushTag,
	onCheckout,
	onCreateBranch,
	onDelete,
	onPush,
	onPushSuccess,
	repositoryId,
	tagName,
	tagRemoteUrl,
}: {
	canCheckoutTag: boolean;
	canCreateBranch: boolean;
	canDeleteTag: boolean;
	canPushTag: boolean;
	onCheckout: () => void;
	onCreateBranch: () => void;
	onDelete: () => void;
	onPush: () => void;
	onPushSuccess: () => void;
	repositoryId: string | null;
	tagName: string;
	tagRemoteUrl?: string | null;
}) {
	const lastPushActivationRef = useRef(0);
	const isPushRunningRef = useRef(false);
	const activatePush = () => {
		const now = performance.now();
		if (now - lastPushActivationRef.current < 250) {
			return;
		}

		lastPushActivationRef.current = now;
		void pushTagFromMenu();
	};
	const pushTagFromMenu = async () => {
		if (repositoryId === null || isPushRunningRef.current) {
			return;
		}

		const selectedRepositoryId = repositoryId;
		const pushToastId = toast.loading(`Pushing tag ${tagName}`);
		isPushRunningRef.current = true;
		try {
			const remotes = await sendRequestWithResponse({
				arguments: { repositoryId: selectedRepositoryId },
				commandType: NativeMessageType.GetRepositoryRemotes,
			});
			await pushWhenSingleRemote(remotes, selectedRepositoryId, pushToastId);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not push tag",
				{ id: pushToastId },
			);
		} finally {
			isPushRunningRef.current = false;
		}
	};
	const pushWhenSingleRemote = async (
		remotes: GitRemote[],
		selectedRepositoryId: string,
		pushToastId: string | number,
	) => {
		if (remotes.length === 0) {
			toast.error("No remotes are configured for this repository.", {
				id: pushToastId,
			});
			return;
		}

		if (remotes.length > 1) {
			toast.dismiss(pushToastId);
			onPush();
			return;
		}

		const remoteName = remotes[0].name;
		await sendRequestWithResponse({
			arguments: {
				remoteName,
				repositoryId: selectedRepositoryId,
				tagName,
			},
			commandType: NativeMessageType.PushTag,
		});
		toast.success(`Pushed tag ${tagName} to ${remoteName}`, {
			id: pushToastId,
		});
		onPushSuccess();
	};
	const handlePushKeyDown = (event: KeyboardEvent<HTMLButtonElement>) => {
		if (event.key !== "Enter" && event.key !== " ") {
			return;
		}

		event.preventDefault();
		activatePush();
	};
	const handlePushMouseUp = (event: MouseEvent<HTMLButtonElement>) => {
		event.preventDefault();
		activatePush();
	};
	const handlePushPointerUp = (event: PointerEvent<HTMLButtonElement>) => {
		event.preventDefault();
		activatePush();
	};
	return (
		<>
			{canCheckoutTag ? (
				<ContextMenuItem onClick={onCheckout}>
					<GitCommitHorizontal />
					Checkout tag
				</ContextMenuItem>
			) : null}
			{canCreateBranch ? (
				<ContextMenuItem onClick={onCreateBranch}>
					<GitBranch />
					Create branch from tag
				</ContextMenuItem>
			) : null}
			<ContextMenuItem
				onClick={() => void copyToClipboard(tagName, "Tag name")}
			>
				<Copy />
				Copy tag name
			</ContextMenuItem>
			{canPushTag ? (
				<ContextMenuItem className="p-0">
					<button
						className="flex w-full items-center gap-1.5 rounded-md px-1.5 py-1 text-left"
						data-testid="push-tag-menu-item"
						onClick={activatePush}
						onKeyDown={handlePushKeyDown}
						onMouseUp={handlePushMouseUp}
						onPointerUp={handlePushPointerUp}
						title="Push tag"
						type="button"
					>
						<Upload />
						Push tag
					</button>
				</ContextMenuItem>
			) : null}
			{tagRemoteUrl ? (
				<>
					<ContextMenuItem
						onClick={() => void copyToClipboard(tagRemoteUrl, "Remote tag URL")}
					>
						<Copy />
						Copy remote tag URL
					</ContextMenuItem>
					<ContextMenuItem onClick={() => openRemoteTag(tagRemoteUrl)}>
						<ExternalLink />
						Open tag on remote
					</ContextMenuItem>
				</>
			) : null}
			{canDeleteTag ? (
				<ContextMenuItem onClick={onDelete} variant="destructive">
					<Trash2 />
					Delete local tag
				</ContextMenuItem>
			) : null}
		</>
	);
}

function openRemoteTag(remoteUrl: string) {
	window.open(remoteUrl, "_blank", "noopener,noreferrer");
	toast.success("Opened tag on remote");
}
