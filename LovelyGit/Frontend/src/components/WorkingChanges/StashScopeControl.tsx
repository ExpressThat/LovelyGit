import { motion, useReducedMotion } from "@/lib/motion";
import { cn } from "@/lib/utils";

export function StashScopeControl({
	onSelectedOnlyChange,
	selectedCount,
	selectedOnly,
}: {
	onSelectedOnlyChange: (selectedOnly: boolean) => void;
	selectedCount: number;
	selectedOnly: boolean;
}) {
	return (
		<fieldset className="grid gap-1.5">
			<legend className="text-sm font-medium">Changes to stash</legend>
			<div className="grid grid-cols-2 rounded-lg border bg-background p-1">
				<ScopeButton
					active={!selectedOnly}
					label="All changes"
					onClick={() => onSelectedOnlyChange(false)}
				/>
				<ScopeButton
					active={selectedOnly}
					disabled={selectedCount === 0}
					label={`Selected files (${selectedCount})`}
					onClick={() => onSelectedOnlyChange(true)}
				/>
			</div>
			<p className="text-xs text-muted-foreground">
				{selectedOnly
					? "Git will stash staged and unstaged changes for each selected path."
					: "Every eligible working-tree change will be included."}
			</p>
		</fieldset>
	);
}

function ScopeButton({
	active,
	disabled = false,
	label,
	onClick,
}: {
	active: boolean;
	disabled?: boolean;
	label: string;
	onClick: () => void;
}) {
	const reduceMotion = useReducedMotion();
	return (
		<button
			aria-pressed={active}
			className={cn(
				"relative isolate h-8 rounded-md px-2 text-xs font-medium transition-colors",
				active
					? "text-foreground"
					: "text-muted-foreground hover:text-foreground",
				"disabled:pointer-events-none disabled:opacity-40",
			)}
			disabled={disabled}
			onClick={onClick}
			type="button"
		>
			{active ? (
				<motion.span
					className="absolute inset-0 -z-10 rounded-md border bg-card shadow-sm"
					layoutId="stash-scope-selection"
					transition={
						reduceMotion
							? { duration: 0 }
							: { type: "spring", bounce: 0.12, duration: 0.3 }
					}
				/>
			) : null}
			{label}
		</button>
	);
}
