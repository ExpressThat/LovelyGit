import { Switch as SwitchPrimitive } from "@base-ui/react/switch";
import { cn } from "@/lib/utils";

export function LovelySwitch({
	className,
	...props
}: SwitchPrimitive.Root.Props) {
	return (
		<SwitchPrimitive.Root
			className={cn(
				"group/lovely-switch relative inline-flex h-6 w-11 shrink-0 cursor-pointer items-center rounded-full border border-border/70 bg-input p-0.5 shadow-inner outline-none transition-[background-color,border-color,box-shadow] duration-150 data-checked:border-primary/80 data-checked:bg-primary data-disabled:cursor-not-allowed data-disabled:opacity-50 focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background",
				className,
			)}
			{...props}
		>
			<SwitchPrimitive.Thumb className="pointer-events-none block size-5 translate-x-0 rounded-full bg-background shadow-sm ring-1 ring-black/5 transition-transform duration-150 ease-out data-checked:translate-x-5 dark:data-checked:bg-primary-foreground" />
		</SwitchPrimitive.Root>
	);
}
