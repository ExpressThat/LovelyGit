import {
	Download,
	GitPullRequestArrow,
	type LucideIcon,
	Upload,
} from "lucide-react";
import type { GitPullMode, RemotePrimaryAction } from "@/generated/types";
import {
	NativeMessageType,
	type NativeMessageTypesWithRequest,
} from "@/lib/nativeMessaging";

export type RemoteAction = {
	commandType: NativeMessageTypesWithRequest;
	icon: LucideIcon;
	label: string;
	menuLabel: string;
	pullMode: GitPullMode;
	toolbarLabel: string;
	value: RemotePrimaryAction;
};

export const defaultableRemoteActions: RemoteAction[] = [
	remoteAction(NativeMessageType.FetchRepository, Download, "Fetch", "Fetch"),
	remoteAction(
		NativeMessageType.PullRepository,
		GitPullRequestArrow,
		"Pull",
		"Pull",
	),
	remoteAction(
		NativeMessageType.PullRepository,
		GitPullRequestArrow,
		"Pull",
		"PullFastForwardOnly",
		"FastForwardOnly",
		"Pull (fast-forward only)",
		"Pull ff-only",
	),
	remoteAction(
		NativeMessageType.PullRepository,
		GitPullRequestArrow,
		"Pull",
		"PullRebase",
		"Rebase",
		"Pull (rebase)",
		"Pull rebase",
	),
];

export const pushRemoteAction: RemoteAction = remoteAction(
	NativeMessageType.PushRepository,
	Upload,
	"Push",
	"Push",
);

const defaultableValues = new Set<RemotePrimaryAction>(
	defaultableRemoteActions.map((action) => action.value),
);

export function normalizePrimaryAction(value: RemotePrimaryAction) {
	return defaultableValues.has(value) ? value : "Fetch";
}

export function primaryIconTitle(action: RemoteAction) {
	return action.value === "Fetch" ? "Fetch all" : action.menuLabel;
}

function remoteAction(
	commandType: NativeMessageTypesWithRequest,
	icon: LucideIcon,
	label: string,
	value: RemotePrimaryAction,
	pullMode: GitPullMode = "Merge",
	menuLabel = label === "Fetch" ? "Fetch All" : label,
	toolbarLabel = label,
): RemoteAction {
	return { commandType, icon, label, menuLabel, pullMode, toolbarLabel, value };
}
