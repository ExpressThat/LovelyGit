import { motion, useReducedMotion } from "motion/react";
import { shortHash } from "../CommitGraph/utils/format";

export function CommitParentSelector({
	busy,
	onChange,
	parents,
	selectedIndex,
}: {
	busy: boolean;
	onChange: (index: number) => void;
	parents: string[];
	selectedIndex: number;
}) {
	const reduceMotion = useReducedMotion();
	if (parents.length < 2) return null;
	return (
		<section className="space-y-1.5">
			<p className="font-medium text-muted-foreground text-xs">
				Compare changes against
			</p>
			<fieldset className="flex min-w-0 gap-1 rounded-lg border bg-card p-1">
				<legend className="sr-only">Merge commit parent</legend>
				{parents.map((parent, index) => {
					const selected = selectedIndex === index;
					return (
						<motion.button
							aria-pressed={selected}
							className="relative min-w-0 flex-1 rounded-md px-2 py-1.5 text-xs disabled:opacity-60"
							disabled={busy}
							key={parent}
							onClick={() => onChange(index)}
							title={`Compare with parent ${index + 1}: ${parent}`}
							type="button"
							whileTap={reduceMotion ? undefined : { scale: 0.98 }}
						>
							{selected ? (
								<motion.span
									className="absolute inset-0 rounded-md bg-accent ring-1 ring-ring/30"
									layoutId="commit-parent-selection"
									transition={{ duration: reduceMotion ? 0 : 0.18 }}
								/>
							) : null}
							<span className="relative block truncate">
								Parent {index + 1} · {shortHash(parent)}
							</span>
						</motion.button>
					);
				})}
			</fieldset>
		</section>
	);
}
