import {
	ChevronDown,
	Download,
	GitPullRequestArrow,
	Upload,
} from "lucide-react";
import { useMemo, useState } from "react";
import { toast } from "sonner";
import {
	showConflictWorkspaceIfNeeded,
	showGitActionError,
} from "@/components/Conflicts/ConflictTransition";
import { Button } from "@/components/ui/button";
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuGroup,
	DropdownMenuLabel,
	DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import type { GitPullMode, RemotePrimaryAction } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import {
	NativeMessageType,
	type NativeMessageTypesWithRequest,
} from "@/lib/nativeMessaging";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import { RemoteDefaultRow } from "./RemoteDefaultRow";

export type RemoteAction = {
	commandType: NativeMessageTypesWithRequest;
	icon: typeof Download;
	label: string;
	menuLabel: string;
	pullMode: GitPullMode;
	toolbarLabel: string;
	value: RemotePrimaryAction;
};

const defaultableActions: RemoteAction[] = [
	{
		commandType: NativeMessageType.FetchRepository,
		icon: Download,
		label: "Fetch",
		menuLabel: "Fetch All",
		pullMode: "Merge",
		toolbarLabel: "Fetch",
		value: "Fetch",
	},
	{
		commandType: NativeMessageType.PullRepository,
		icon: GitPullRequestArrow,
		label: "Pull",
		menuLabel: "Pull (fast-forward if possible)",
		pullMode: "Merge",
		toolbarLabel: "Pull",
		value: "Pull",
	},
	{
		commandType: NativeMessageType.PullRepository,
		icon: GitPullRequestArrow,
		label: "Pull",
		menuLabel: "Pull (fast-forward only)",
		pullMode: "FastForwardOnly",
		toolbarLabel: "Pull ff-only",
		value: "PullFastForwardOnly",
	},
	{
		commandType: NativeMessageType.PullRepository,
		icon: GitPullRequestArrow,
		label: "Pull",
		menuLabel: "Pull (rebase)",
		pullMode: "Rebase",
		toolbarLabel: "Pull rebase",
		value: "PullRebase",
	},
];

const pushAction: RemoteAction = {
	commandType: NativeMessageType.PushRepository,
	icon: Upload,
	label: "Push",
	menuLabel: "Push",
	pullMode: "Merge",
	toolbarLabel: "Push",
	value: "Push",
};

const defaultableValues = new Set<RemotePrimaryAction>(
	defaultableActions.map((action) => action.value),
);

function normalizePrimaryAction(value: RemotePrimaryAction) {
	return defaultableValues.has(value) ? value : "Fetch";
}

function primaryIconTitle(action: RemoteAction) {
	if (action.value === "Fetch") {
		return "Fetch all";
	}

	return action.menuLabel;
}

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
	const primary = useMemo(
		() =>
			defaultableActions.find((action) => action.value === primaryAction) ??
			defaultableActions[0],
		[primaryAction],
	);
	const isBusy = busyAction !== null;
	const Icon = primary.icon;

	const runAction = async (action: RemoteAction) => {
		if (!repositoryId || isBusy) {
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
						repositoryId,
					},
				},
				{
					timeoutMs: gitMutationTimeoutMs,
				},
			);
			toast.success(`${action.label} complete`, { id: toastId });
		} catch (error) {
			if (
				action.commandType === NativeMessageType.PullRepository &&
				(await showConflictWorkspaceIfNeeded({
					repositoryId,
					toastId,
				}))
			) {
				return;
			}
			showGitActionError(error, `${action.label} failed`, toastId);
		} finally {
			setBusyAction(null);
		}
	};

	return (
		<div className="inline-flex items-center gap-1">
			<div className="inline-flex h-8 overflow-hidden rounded-md border bg-background">
				<Button
					aria-label={primaryIconTitle(primary)}
					className="h-full min-w-24 rounded-none border-0 px-2"
					disabled={!repositoryId || isBusy}
					onClick={() => void runAction(primary)}
					size="sm"
					title={primaryIconTitle(primary)}
					type="button"
					variant="ghost"
				>
					<Icon
						aria-hidden="true"
						className={
							busyAction === primary.value ? "animate-pulse" : undefined
						}
					/>
					<span>{primary.toolbarLabel}</span>
				</Button>
				<DropdownMenu open={open} onOpenChange={setOpen}>
					<DropdownMenuTrigger
						aria-label="Choose fetch or pull default"
						className="inline-flex h-full w-7 items-center justify-center border-l text-muted-foreground hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50"
						disabled={!repositoryId || isBusy}
						title="Choose fetch or pull default"
					>
						<span className="sr-only">Choose fetch or pull default</span>
						<ChevronDown aria-hidden="true" className="size-3.5" />
					</DropdownMenuTrigger>
					<DropdownMenuContent align="start" className="min-w-72 p-0">
						<DropdownMenuGroup>
							<DropdownMenuLabel className="px-3 py-2 text-sm normal-case leading-snug">
								Choose which fetch or pull action the main toolbar button runs
								by default
							</DropdownMenuLabel>
							<div className="py-1" role="radiogroup">
								{defaultableActions.map((action) => (
									<RemoteDefaultRow
										action={action}
										isDefault={primaryAction === action.value}
										isDisabled={!repositoryId || isBusy}
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
				className="h-8"
				disabled={!repositoryId || isBusy}
				onClick={() => void runAction(pushAction)}
				size="sm"
				title="Push"
				type="button"
				variant="ghost"
			>
				<Upload
					aria-hidden="true"
					className={busyAction === "Push" ? "animate-pulse" : undefined}
				/>
				<span>Push</span>
			</Button>
		</div>
	);
}
