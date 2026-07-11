import type { ReactNode } from "react";
import { Checkbox } from "@/components/ui/checkbox";

interface MutationOptionToggleProps {
	accessibleName: string;
	checked: boolean;
	children: ReactNode;
	disabled?: boolean;
	icon?: ReactNode;
	id: string;
	onCheckedChange: (checked: boolean) => void;
}

export function MutationOptionToggle({
	accessibleName,
	checked,
	children,
	disabled = false,
	icon,
	id,
	onCheckedChange,
}: MutationOptionToggleProps) {
	return (
		<div className="flex items-center gap-2 text-sm">
			<Checkbox
				aria-label={accessibleName}
				checked={checked}
				disabled={disabled}
				onCheckedChange={onCheckedChange}
			/>
			{icon}
			<button
				className="text-left"
				disabled={disabled}
				id={id}
				onClick={() => onCheckedChange(!checked)}
				type="button"
			>
				{children}
			</button>
		</div>
	);
}
