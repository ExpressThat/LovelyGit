import {
	GitCommitHorizontal,
	GitCompareArrows,
	Undo2,
	X,
} from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import { AnimatePresence, motion, useReducedMotion } from "@/lib/motion";
import { CommitPatchSeriesMenu } from "./CommitPatchSeriesMenu";

export function CommitMultiSelectionBar({
	count,
	cherryPickDisabled,
	revertDisabled,
	onCherryPick,
	onClear,
	onCompare,
	onCopyPatchSeries,
	onRevert,
	onSavePatchSeries,
	seriesBusyAction,
}: {
	count: number;
	cherryPickDisabled: boolean;
	revertDisabled: boolean;
	onCherryPick: () => void;
	onClear: () => void;
	onCompare: () => void;
	onCopyPatchSeries: () => void;
	onRevert: () => void;
	onSavePatchSeries: () => void;
	seriesBusyAction: "copy" | "save" | null;
}) {
	const reduceMotion = useReducedMotion();
	return (
		<AnimatePresence initial={false}>
			{count > 0 ? (
				<motion.div
					animate={{ height: 34, opacity: 1, y: 0 }}
					className="flex items-center gap-2 overflow-hidden border-b bg-primary/8 px-2 text-xs"
					exit={{ height: 0, opacity: 0, y: reduceMotion ? 0 : -4 }}
					initial={{ height: 0, opacity: 0, y: reduceMotion ? 0 : -4 }}
				>
					<span className="font-semibold text-foreground">
						{count} selected
					</span>
					<span className="text-muted-foreground">
						Ctrl toggles · Shift selects a range
					</span>
					<div className="ml-auto flex gap-1">
						{count > 1 ? (
							<CommitPatchSeriesMenu
								busyAction={seriesBusyAction}
								count={count}
								onCopy={onCopyPatchSeries}
								onSave={onSavePatchSeries}
							/>
						) : null}
						{count === 2 ? (
							<Button onClick={onCompare} size="xs" variant="secondary">
								<GitCompareArrows aria-hidden="true" /> Compare
							</Button>
						) : null}
						<Button
							disabled={cherryPickDisabled}
							onClick={onCherryPick}
							size="xs"
							variant="secondary"
						>
							<GitCommitHorizontal aria-hidden="true" /> Cherry-pick
						</Button>
						<Button
							disabled={revertDisabled}
							onClick={onRevert}
							size="xs"
							variant="secondary"
						>
							<Undo2 aria-hidden="true" /> Revert
						</Button>
						<Button
							aria-label="Clear commit selection"
							onClick={onClear}
							size="icon-xs"
							variant="ghost"
						>
							<X aria-hidden="true" />
						</Button>
					</div>
				</motion.div>
			) : null}
		</AnimatePresence>
	);
}
