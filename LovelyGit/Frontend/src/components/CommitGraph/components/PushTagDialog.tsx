import { Upload } from "lucide-react";
import {
	type FormEvent,
	type MouseEvent,
	useCallback,
	useEffect,
	useState,
} from "react";
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
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from "@/components/ui/select";
import type { GitRemote } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";

type PushTagState = "idle" | "loading" | "choosing" | "pushing";

export function PushTagDialog({
	isOpen,
	onOpenChange,
	onSuccess,
	repositoryId,
	tagName,
}: {
	isOpen: boolean;
	onOpenChange: (isOpen: boolean) => void;
	onSuccess: () => void;
	repositoryId: string | null;
	tagName: string;
}) {
	const [remotes, setRemotes] = useState<GitRemote[]>([]);
	const [selectedRemoteName, setSelectedRemoteName] = useState("");
	const [state, setState] = useState<PushTagState>("idle");
	const isPushing = state === "pushing";
	const canPush =
		!isPushing && repositoryId !== null && selectedRemoteName.length > 0;

	const pushTag = useCallback(
		async (remoteName: string, keepChooserOpen: boolean) => {
			if (repositoryId === null) {
				return;
			}

			setState("pushing");
			try {
				await sendRequestWithResponse({
					arguments: {
						remoteName,
						repositoryId,
						tagName,
					},
					commandType: NativeMessageType.PushTag,
				});
				toast.success(`Pushed tag ${tagName} to ${remoteName}`);
				onSuccess();
				onOpenChange(false);
			} catch (error) {
				toast.error(
					error instanceof Error ? error.message : "Could not push tag",
				);
				if (keepChooserOpen) {
					setState("choosing");
					return;
				}

				onOpenChange(false);
			}
		},
		[onOpenChange, onSuccess, repositoryId, tagName],
	);

	useEffect(() => {
		if (!isOpen) {
			setState("idle");
			setRemotes([]);
			setSelectedRemoteName("");
			return;
		}

		if (repositoryId === null) {
			toast.error("Select a repository before pushing a tag.");
			onOpenChange(false);
			return;
		}

		let isActive = true;
		setState("loading");
		void sendRequestWithResponse({
			arguments: { repositoryId },
			commandType: NativeMessageType.GetRepositoryRemotes,
		})
			.then((loadedRemotes) => {
				if (!isActive) {
					return;
				}

				if (loadedRemotes.length === 0) {
					toast.error("No remotes are configured for this repository.");
					onOpenChange(false);
					return;
				}

				if (loadedRemotes.length === 1) {
					void pushTag(loadedRemotes[0].name, false);
					return;
				}

				setRemotes(loadedRemotes);
				setSelectedRemoteName(preferredRemoteName(loadedRemotes));
				setState("choosing");
			})
			.catch((error) => {
				if (!isActive) {
					return;
				}

				toast.error(
					error instanceof Error ? error.message : "Could not load remotes",
				);
				onOpenChange(false);
			});

		return () => {
			isActive = false;
		};
	}, [isOpen, onOpenChange, pushTag, repositoryId]);

	const submitPush = () => {
		if (canPush) {
			void pushTag(selectedRemoteName, true);
		}
	};
	const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
		event.preventDefault();
		submitPush();
	};
	const handlePushClick = (event: MouseEvent<HTMLButtonElement>) => {
		event.preventDefault();
		submitPush();
	};
	const shouldShowChooser = isOpen && remotes.length > 1 && state !== "loading";

	if (!shouldShowChooser) {
		return null;
	}

	return (
		<Dialog
			onOpenChange={(open) => {
				if (!open && !isPushing) {
					onOpenChange(false);
				}
			}}
			open={true}
		>
			<DialogContent>
				<form className="grid gap-4" onSubmit={handleSubmit}>
					<DialogHeader>
						<DialogTitle className="flex items-center gap-2">
							<Upload aria-hidden="true" />
							Push tag
						</DialogTitle>
						<DialogDescription>
							Choose one of this repository's configured remotes for {tagName}.
						</DialogDescription>
					</DialogHeader>
					<Select
						disabled={isPushing}
						onValueChange={(value) => {
							if (value !== null) {
								setSelectedRemoteName(value);
							}
						}}
						value={selectedRemoteName}
					>
						<SelectTrigger aria-label="Remote" className="w-full">
							<SelectValue placeholder="Select remote" />
						</SelectTrigger>
						<SelectContent>
							{remotes.map((remote) => (
								<SelectItem key={remote.name} value={remote.name}>
									<span className="font-medium">{remote.name}</span>
									<span className="truncate text-muted-foreground">
										{remote.url}
									</span>
								</SelectItem>
							))}
						</SelectContent>
					</Select>
					<DialogFooter>
						<Button
							disabled={isPushing}
							onClick={() => onOpenChange(false)}
							type="button"
							variant="outline"
						>
							Cancel
						</Button>
						<Button disabled={!canPush} onClick={handlePushClick} type="submit">
							{isPushing ? "Pushing" : "Push tag"}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}

function preferredRemoteName(remotes: GitRemote[]) {
	return (
		remotes.find((remote) => remote.name === "origin")?.name ?? remotes[0].name
	);
}
