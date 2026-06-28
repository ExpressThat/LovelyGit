import {
	AlertTriangle,
	Binary,
	GitMerge,
	GitPullRequestArrow,
	RotateCcw,
} from "lucide-react";
import {
	Tooltip,
	TooltipContent,
	TooltipProvider,
	TooltipTrigger,
} from "@/components/ui/tooltip";
import { GitOperationKind, type GitOperationState } from "@/generated/types";

const operationIcons = {
	[GitOperationKind.Bisect]: Binary,
	[GitOperationKind.CherryPick]: GitPullRequestArrow,
	[GitOperationKind.Merge]: GitMerge,
	[GitOperationKind.None]: AlertTriangle,
	[GitOperationKind.Rebase]: GitPullRequestArrow,
	[GitOperationKind.Revert]: RotateCcw,
};

export function GitOperationStateIndicator({
	state,
}: {
	state: GitOperationState | null;
}) {
	if (!state?.isInProgress) {
		return null;
	}

	const Icon = operationIcons[state.kind] ?? AlertTriangle;
	return (
		<TooltipProvider>
			<Tooltip>
				<TooltipTrigger
					aria-label={state.label}
					className="inline-flex h-7 items-center gap-1.5 rounded-md border border-amber-500/40 bg-amber-500/10 px-2 text-amber-700 text-xs font-medium outline-none hover:bg-amber-500/15 focus-visible:ring-2 focus-visible:ring-ring dark:text-amber-200"
					title={state.label}
				>
					<Icon aria-hidden="true" className="size-3.5" />
					<span>{state.label}</span>
				</TooltipTrigger>
				<TooltipContent side="bottom">{state.description}</TooltipContent>
			</Tooltip>
		</TooltipProvider>
	);
}
