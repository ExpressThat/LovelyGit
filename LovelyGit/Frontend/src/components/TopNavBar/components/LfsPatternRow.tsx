import {
	FileArchive,
	LoaderCircle,
	Trash2,
} from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import { motion, useReducedMotion } from "@/lib/motion";

export function LfsPatternRow({
	animateRow = true,
	busy,
	disabled,
	onRemove,
	pattern,
	position,
}: {
	animateRow?: boolean;
	busy: boolean;
	disabled: boolean;
	onRemove: () => void;
	pattern: string;
	position?: {
		index: number;
		measureElement: (node: Element | null) => void;
		start: number;
	};
}) {
	const reduceMotion = useReducedMotion();
	const shouldAnimate = animateRow && !reduceMotion;
	return (
		<motion.article
			animate={{ opacity: 1, y: 0 }}
			className="flex min-w-0 items-center gap-3 rounded-lg border bg-card px-3 py-2.5"
			data-index={position?.index}
			initial={shouldAnimate ? { opacity: 0, y: 4 } : false}
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
		</motion.article>
	);
}
