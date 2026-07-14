import {
	CircleCheck,
	CircleX,
	LoaderCircle,
	RotateCcw,
	SearchCode,
	SkipForward,
} from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import type { GitBisectAction, GitBisectState } from "@/generated/types";
import { AnimatePresence, motion } from "@/lib/motion";

export function BisectSessionContent({
	busyAction,
	isLoading,
	onRun,
	state,
}: {
	busyAction: GitBisectAction | null;
	isLoading: boolean;
	onRun: (action: GitBisectAction) => void;
	state: GitBisectState | null;
}) {
	return (
		<AnimatePresence mode="wait">
			{isLoading && !state ? (
				<Loading key="loading" />
			) : state?.isActive ? (
				<ActiveSession
					busyAction={busyAction}
					key="active"
					onRun={onRun}
					state={state}
				/>
			) : (
				<EmptySession key="empty" />
			)}
		</AnimatePresence>
	);
}

function ActiveSession({
	busyAction,
	onRun,
	state,
}: {
	busyAction: GitBisectAction | null;
	onRun: (action: GitBisectAction) => void;
	state: GitBisectState;
}) {
	const busy = busyAction !== null;
	return (
		<motion.div animate={{ opacity: 1, y: 0 }} initial={{ opacity: 0, y: 5 }}>
			{state.firstBadCommit ? (
				<BisectResult state={state} />
			) : (
				<CurrentRevision state={state} />
			)}
			<div className="mt-4 flex flex-wrap justify-end gap-2">
				{!state.firstBadCommit ? (
					<>
						<Action
							action="MarkGood"
							busy={busyAction}
							disabled={busy}
							onRun={onRun}
						/>
						<Action
							action="MarkBad"
							busy={busyAction}
							disabled={busy}
							onRun={onRun}
						/>
						<Action
							action="Skip"
							busy={busyAction}
							disabled={busy}
							onRun={onRun}
						/>
					</>
				) : null}
				<Action
					action="Reset"
					busy={busyAction}
					disabled={busy}
					onRun={onRun}
				/>
			</div>
		</motion.div>
	);
}

function CurrentRevision({ state }: { state: GitBisectState }) {
	return (
		<div className="rounded-lg border bg-card p-4">
			<div className="mb-2 flex items-center justify-between gap-3">
				<span className="font-medium text-sm">Revision under test</span>
				<span className="rounded-full bg-primary/15 px-2 py-0.5 text-primary text-xs">
					Testing
				</span>
			</div>
			<p className="font-medium">
				{state.currentSubject || "Commit without a subject"}
			</p>
			<p className="mt-1 font-mono text-muted-foreground text-xs">
				{short(state.currentCommit)}
			</p>
			<p className="mt-3 text-muted-foreground text-xs">
				Run your test, then mark this revision good or bad. Git will select the
				next midpoint.
			</p>
		</div>
	);
}

function BisectResult({ state }: { state: GitBisectState }) {
	return (
		<motion.div
			animate={{ scale: 1 }}
			aria-label="First bad commit found"
			className="rounded-lg border border-primary/30 bg-primary/10 p-4"
			initial={{ scale: 0.98 }}
		>
			<div className="flex items-center gap-2 font-medium text-primary">
				<CircleCheck className="size-5" /> First bad commit found
			</div>
			<p className="mt-2 break-all font-mono text-xs">{state.firstBadCommit}</p>
			<p className="mt-2 text-muted-foreground text-xs">
				Reset the session to return to{" "}
				{state.startingReference || "the starting revision"}.
			</p>
		</motion.div>
	);
}

function EmptySession() {
	return (
		<motion.div
			animate={{ opacity: 1 }}
			className="grid place-items-center rounded-lg border border-dashed px-6 py-10 text-center"
			initial={{ opacity: 0 }}
		>
			<SearchCode className="mb-3 size-8 text-muted-foreground" />
			<p className="font-medium">No active bisect session</p>
			<p className="mt-1 max-w-sm text-muted-foreground text-sm">
				Right-click an earlier known-good commit in the graph and choose “Start
				bisect” while the current HEAD demonstrates the bug.
			</p>
		</motion.div>
	);
}

function Loading() {
	return (
		<div className="flex items-center justify-center gap-2 py-10 text-muted-foreground text-sm">
			<LoaderCircle className="animate-spin" /> Reading bisect state…
		</div>
	);
}

function Action({
	action,
	busy,
	disabled,
	onRun,
}: {
	action: GitBisectAction;
	busy: GitBisectAction | null;
	disabled: boolean;
	onRun: (action: GitBisectAction) => void;
}) {
	const details = actionDetails(action);
	const Icon = details.icon;
	return (
		<Button
			aria-label={details.ariaLabel}
			disabled={disabled}
			onClick={() => onRun(action)}
			variant={actionVariant(action)}
		>
			{busy === action ? <LoaderCircle className="animate-spin" /> : <Icon />}
			{details.label}
		</Button>
	);
}

function actionVariant(action: GitBisectAction) {
	if (action === "MarkBad") return "destructive" as const;
	if (action === "Skip") return "secondary" as const;
	if (action === "Reset") return "outline" as const;
	return "default" as const;
}

function actionDetails(action: GitBisectAction) {
	if (action === "MarkGood") {
		return {
			ariaLabel: "Mark current revision good",
			icon: CircleCheck,
			label: "Good",
		};
	}
	if (action === "MarkBad") {
		return {
			ariaLabel: "Mark current revision bad",
			icon: CircleX,
			label: "Bad",
		};
	}
	if (action === "Skip") {
		return {
			ariaLabel: "Skip current revision",
			icon: SkipForward,
			label: "Skip",
		};
	}
	return { ariaLabel: "Reset bisect session", icon: RotateCcw, label: "Reset" };
}

function short(hash: string | null) {
	return hash?.slice(0, 12) ?? "unknown";
}
