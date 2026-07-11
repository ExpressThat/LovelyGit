import { motion, useReducedMotion } from "motion/react";
import {
	FileArchive,
	LoaderCircle,
	Trash2,
} from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";

export function LfsPatternRow({
	busy,
	disabled,
	onRemove,
	pattern,
}: {
	busy: boolean;
	disabled: boolean;
	onRemove: () => void;
	pattern: string;
}) {
	const reduceMotion = useReducedMotion();
	return (
		<motion.div
			animate={{ opacity: 1, y: 0 }}
			className="flex min-w-0 items-center gap-3 rounded-lg border bg-card px-3 py-2.5"
			initial={{ opacity: 0, y: reduceMotion ? 0 : 4 }}
			layout={!reduceMotion}
		>
			<div className="grid size-8 shrink-0 place-items-center rounded-md bg-primary/10 text-primary">
				<FileArchive aria-hidden="true" className="size-4" />
			</div>
			<code className="min-w-0 flex-1 truncate text-xs" title={pattern}>
				{pattern}
			</code>
			<Button
				aria-label={`Stop tracking ${pattern} with Git LFS`}
				disabled={disabled}
				onClick={onRemove}
				size="icon-sm"
				title="Stop tracking this pattern"
				variant="ghost"
			>
				{busy ? (
					<LoaderCircle aria-hidden="true" className="animate-spin" />
				) : (
					<Trash2 aria-hidden="true" />
				)}
			</Button>
		</motion.div>
	);
}
