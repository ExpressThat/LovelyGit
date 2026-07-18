import {
	Box,
	LoaderCircle,
	RefreshCw,
	Unplug,
	WandSparkles,
} from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import type { GitSubmodule, SubmoduleAction } from "@/generated/types";
import { motion, useReducedMotion } from "@/lib/motion";

export function SubmoduleRow({
	animateRow = true,
	busy,
	disabled,
	onDeinitialize,
	onRun,
	position,
	submodule,
}: {
	animateRow?: boolean;
	busy: boolean;
	disabled: boolean;
	onDeinitialize: () => void;
	onRun: (action: SubmoduleAction) => void;
	position?: {
		index: number;
		measureElement: (node: Element | null) => void;
		start: number;
	};
	submodule: GitSubmodule;
}) {
	const uninitialized = submodule.state === "Uninitialized";
	const reduceMotion = useReducedMotion();
	const shouldAnimate = animateRow && !reduceMotion;
	return (
		<motion.article
			animate={{ opacity: 1, y: 0 }}
			className="grid gap-3 rounded-lg border bg-card p-3"
			data-index={position?.index}
			initial={shouldAnimate ? { opacity: 0, y: 6 } : false}
			layout={shouldAnimate}
			ref={position?.measureElement}
			style={
				position
					? {
							left: 0,
							position: "absolute",
							right: 0,
							top: position.start,
						}
					: undefined
			}
		>
			<div className="flex min-w-0 items-start gap-3">
				<Box className="mt-0.5 size-5 shrink-0 text-primary" />
				<div className="min-w-0 flex-1">
					<div className="flex items-center gap-2">
						<span className="truncate font-medium">{submodule.name}</span>
						<StateBadge state={submodule.state} />
					</div>
					<div className="truncate font-mono text-muted-foreground text-xs">
						{submodule.path}
					</div>
					<div
						className="truncate text-muted-foreground text-xs"
						title={submodule.url}
					>
						{submodule.url}
					</div>
				</div>
			</div>
			<div className="flex flex-wrap justify-end gap-2">
				{uninitialized ? (
					<Button
						disabled={disabled}
						onClick={() => onRun("Initialize")}
						size="sm"
					>
						{busy ? (
							<LoaderCircle className="animate-spin" />
						) : (
							<WandSparkles />
						)}
						Initialize
					</Button>
				) : (
					<>
						<Button
							disabled={disabled}
							onClick={() => onRun("Synchronize")}
							size="sm"
							variant="outline"
						>
							<RefreshCw /> Sync URLs
						</Button>
						<Button
							disabled={disabled}
							onClick={() => onRun("Update")}
							size="sm"
						>
							{busy ? <LoaderCircle className="animate-spin" /> : <RefreshCw />}
							Update
						</Button>
						<Button
							aria-label={`Deinitialize ${submodule.name}`}
							disabled={disabled}
							onClick={onDeinitialize}
							size="sm"
							variant="destructive"
						>
							<Unplug /> Deinitialize…
						</Button>
					</>
				)}
			</div>
		</motion.article>
	);
}

function StateBadge({ state }: { state: GitSubmodule["state"] }) {
	const label =
		state === "DifferentCommit"
			? "Different commit"
			: state === "MissingFromHead"
				? "Not in HEAD"
				: state;
	return (
		<span className="shrink-0 rounded-full bg-secondary px-2 py-0.5 text-[10px] text-secondary-foreground">
			{label}
		</span>
	);
}
