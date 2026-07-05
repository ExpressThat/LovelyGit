import { ChevronDown } from "lucide-react";
import { useMemo, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuGroup,
	DropdownMenuLabel,
	DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import type { RemotePrimaryAction } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import {
	defaultableRemoteActions,
	normalizePrimaryAction,
	primaryIconTitle,
	pushRemoteAction,
	type RemoteAction,
} from "./RemoteActions";
import { RemoteDefaultRow } from "./RemoteDefaultRow";
import { RemoteTargetSelect } from "./RemoteTargetSelect";
import { useRemoteTargets } from "./RemoteTargets";

export function RemoteActionsControl({
	repositoryId,
}: {
	repositoryId: string | null;
}) {
	const [busyAction, setBusyAction] = useState<RemotePrimaryAction | null>(
		null,
	);
	const [open, setOpen] = useState(false);
	const primaryAction = normalizePrimaryAction(
		useSetting("RemotePrimaryAction"),
	);
	const remoteTargets = useRemoteTargets(repositoryId);
	const primary = useMemo(
		() =>
			defaultableRemoteActions.find(
				(action) => action.value === primaryAction,
			) ?? defaultableRemoteActions[0],
		[primaryAction],
	);
	const isBusy = busyAction !== null;
	const canRunRemoteAction =
		Boolean(repositoryId) &&
		remoteTargets.status === "loaded" &&
		remoteTargets.selectedRemoteName !== null &&
		!isBusy;
	const Icon = primary.icon;
	const PushIcon = pushRemoteAction.icon;

	const runAction = async (action: RemoteAction) => {
		const remoteName = remoteTargets.selectedRemoteName;
		if (!repositoryId || !remoteName || isBusy) {
			return;
		}

		setBusyAction(action.value);
		const toastId = toast.loading(`${action.label} in progress`);
		try {
			await sendRequestWithResponse(
				{
					commandType: action.commandType,
					arguments: {
						pullMode: action.pullMode,
						remoteName,
						repositoryId,
					},
				},
				{
					timeoutMs: gitMutationTimeoutMs,
				},
			);
			toast.success(`${action.label} complete`, { id: toastId });
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : `${action.label} failed`,
				{
					id: toastId,
				},
			);
		} finally {
			setBusyAction(null);
		}
	};

	return (
		<div className="inline-flex items-center gap-1">
			<div className="inline-flex h-9 overflow-hidden rounded-md border bg-background">
				<Button
					aria-label={primaryIconTitle(
						primary,
						remoteTargets.selectedRemoteName,
					)}
					className="h-full min-w-28 rounded-none border-0 px-3"
					disabled={!canRunRemoteAction}
					onClick={() => void runAction(primary)}
					size="sm"
					title={primaryIconTitle(primary, remoteTargets.selectedRemoteName)}
					type="button"
					variant="ghost"
				>
					<Icon
						aria-hidden="true"
						className={`size-6 ${busyAction === primary.value ? "animate-pulse" : ""}`}
					/>
					<span>{primary.toolbarLabel}</span>
				</Button>
				<DropdownMenu open={open} onOpenChange={setOpen}>
					<DropdownMenuTrigger
						aria-label="Choose fetch or pull default"
						className="inline-flex h-full w-8 items-center justify-center border-l text-muted-foreground hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50"
						disabled={!canRunRemoteAction}
						title="Choose fetch or pull default"
					>
						<span className="sr-only">Choose fetch or pull default</span>
						<ChevronDown aria-hidden="true" className="size-5" />
					</DropdownMenuTrigger>
					<DropdownMenuContent align="start" className="min-w-72 p-0">
						<DropdownMenuGroup>
							<DropdownMenuLabel className="px-3 py-2 text-sm normal-case leading-snug">
								Choose which fetch or pull action the main toolbar button runs
								by default
							</DropdownMenuLabel>
							<RemoteTargetSelect
								disabled={isBusy}
								remoteTargets={remoteTargets}
							/>
							<div className="py-1" role="radiogroup">
								{defaultableRemoteActions.map((action) => (
									<RemoteDefaultRow
										action={action}
										isDefault={primaryAction === action.value}
										isDisabled={!canRunRemoteAction}
										key={action.value}
										onChooseDefault={() => {
											void setSetting("RemotePrimaryAction", action.value);
										}}
										onRun={() => {
											setOpen(false);
											void runAction(action);
										}}
									/>
								))}
							</div>
						</DropdownMenuGroup>
					</DropdownMenuContent>
				</DropdownMenu>
			</div>
			<Button
				aria-label="Push"
				className="h-9 px-3"
				disabled={!canRunRemoteAction}
				onClick={() => void runAction(pushRemoteAction)}
				size="sm"
				title={
					remoteTargets.selectedRemoteName
						? `Push to ${remoteTargets.selectedRemoteName}`
						: "No remotes configured"
				}
				type="button"
				variant="ghost"
			>
				<PushIcon
					aria-hidden="true"
					className={`size-6 ${busyAction === "Push" ? "animate-pulse" : ""}`}
				/>
				<span>Push</span>
			</Button>
		</div>
	);
}
