import { CheckCircle2, GitMerge, RefreshCw, ShieldX } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { NativeMessageType } from "@/lib/nativeMessaging";

export function ConflictHeader({
	disabled,
	hasConflicts,
	label,
	onAbort,
	onContinue,
	onRefresh,
}: {
	disabled: boolean;
	hasConflicts: boolean;
	label: string;
	onAbort: () => void;
	onContinue: () => void;
	onRefresh: () => void;
}) {
	return (
		<header className="flex items-center justify-between border-b px-4 py-3">
			<div className="min-w-0">
				<h1 className="flex items-center gap-2 font-semibold text-lg">
					<GitMerge className="size-5 text-amber-500" />
					Conflicts need attention
				</h1>
				<p className="truncate text-muted-foreground text-sm">{label}</p>
			</div>
			<div className="flex items-center gap-2">
				<Button
					aria-label="Refresh conflict state"
					onClick={onRefresh}
					size="sm"
					title="Refresh conflict state"
					variant="outline"
				>
					<RefreshCw />
					Refresh
				</Button>
				<Button
					aria-label="Continue Git operation"
					disabled={disabled || hasConflicts}
					onClick={onContinue}
					size="sm"
					title="Continue Git operation"
				>
					<CheckCircle2 />
					Continue
				</Button>
				<Button
					aria-label="Abort Git operation"
					disabled={disabled}
					onClick={onAbort}
					size="sm"
					title="Abort Git operation"
					variant="destructive"
				>
					<ShieldX />
					Abort
				</Button>
			</div>
		</header>
	);
}

export function ConflictToolbar({
	disabled,
	onMarkResolved,
	onUseOurs,
	onUseTheirs,
	path,
}: {
	disabled: boolean;
	onMarkResolved: () => void;
	onUseOurs: () => void;
	onUseTheirs: () => void;
	path: string | null;
}) {
	return (
		<div className="flex items-center justify-between border-b px-3 py-2">
			<div className="flex min-w-0 items-center gap-2 text-sm">
				<GitMerge className="size-4 text-muted-foreground" />
				<span className="truncate">{path ?? "Select a conflicted file"}</span>
			</div>
			<div className="flex items-center gap-2">
				<Button
					aria-label="Use our version"
					disabled={disabled}
					onClick={onUseOurs}
					size="sm"
					title="Use our version"
					variant="outline"
				>
					Use ours
				</Button>
				<Button
					aria-label="Use their version"
					disabled={disabled}
					onClick={onUseTheirs}
					size="sm"
					title="Use their version"
					variant="outline"
				>
					Use theirs
				</Button>
				<Button
					aria-label="Mark file resolved"
					disabled={disabled}
					onClick={onMarkResolved}
					size="sm"
					title="Mark file resolved"
				>
					Mark resolved
				</Button>
			</div>
		</div>
	);
}

export type ConflictOperationCommand =
	| typeof NativeMessageType.ContinueConflictOperation
	| typeof NativeMessageType.AbortConflictOperation;
