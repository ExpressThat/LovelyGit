import { motion } from "motion/react";
import {
	Layers3,
	ListRestart,
	ShieldAlert,
} from "@/components/icons/lovelyIcons";
import {
	GitResetMode,
	type GitResetMode as GitResetModeValue,
} from "@/generated/types";

export const resetOptions = [
	{
		description: "Move the branch and keep every change staged.",
		icon: Layers3,
		mode: GitResetMode.Soft,
		title: "Soft",
	},
	{
		description: "Move the branch and keep changes unstaged.",
		icon: ListRestart,
		mode: GitResetMode.Mixed,
		title: "Mixed",
	},
	{
		description: "Move the branch and discard tracked changes.",
		icon: ShieldAlert,
		mode: GitResetMode.Hard,
		title: "Hard",
	},
] as const;

export function ResetModeOption({
	mode,
	onSelect,
	option,
	reduceMotion,
}: {
	mode: GitResetModeValue;
	onSelect: (mode: GitResetModeValue) => void;
	option: (typeof resetOptions)[number];
	reduceMotion: boolean;
}) {
	const selected = mode === option.mode;
	return (
		<button
			aria-label={`${option.title} reset: ${option.description}`}
			aria-pressed={selected}
			className="relative grid overflow-hidden rounded-lg border p-3 text-left outline-none transition-colors hover:border-primary/45 focus-visible:ring-2 focus-visible:ring-ring"
			onClick={() => onSelect(option.mode)}
			type="button"
		>
			{selected ? (
				<motion.span
					className="absolute inset-0 bg-primary/10"
					layoutId="reset-mode-selection"
					transition={
						reduceMotion
							? { duration: 0 }
							: { type: "spring", stiffness: 430, damping: 34 }
					}
				/>
			) : null}
			<span className="relative flex items-start gap-3">
				<option.icon
					aria-hidden="true"
					className={
						option.mode === GitResetMode.Hard
							? "mt-0.5 size-4 text-destructive"
							: "mt-0.5 size-4 text-primary"
					}
				/>
				<span className="grid gap-0.5">
					<strong className="text-sm">{option.title}</strong>
					<span className="text-muted-foreground text-xs">
						{option.description}
					</span>
				</span>
			</span>
		</button>
	);
}
