import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { ArrowDown, ArrowUp } from "@/components/icons/lovelyIcons";

export function SyncCountBadge({
	count,
	direction,
	isPartial = false,
}: {
	count: number;
	direction: "incoming" | "outgoing";
	isPartial?: boolean;
}) {
	const reduceMotion = useReducedMotion();
	const Icon = direction === "incoming" ? ArrowDown : ArrowUp;
	return (
		<AnimatePresence initial={false}>
			{count > 0 ? (
				<motion.span
					animate={{ opacity: 1, scale: 1, width: "auto" }}
					aria-hidden="true"
					className="inline-flex h-5 min-w-5 items-center justify-center gap-0.5 rounded-full bg-primary/15 px-1 font-mono text-[10px] font-bold text-primary"
					exit={{ opacity: 0, scale: reduceMotion ? 1 : 0.7, width: 0 }}
					initial={{ opacity: 0, scale: reduceMotion ? 1 : 0.7, width: 0 }}
					key={direction}
					layout
					transition={{ duration: reduceMotion ? 0 : 0.18 }}
				>
					<Icon aria-hidden="true" className="size-2.5" />
					{isPartial ? "≥" : ""}
					{count > 99 ? "99+" : count}
				</motion.span>
			) : null}
		</AnimatePresence>
	);
}

export function syncActionLabel(
	action: string,
	count: number,
	direction: "incoming" | "outgoing",
	isPartial = false,
) {
	if (count <= 0) return action;
	return `${action}, ${isPartial ? "at least " : ""}${count} ${direction} ${count === 1 ? "commit" : "commits"}`;
}
