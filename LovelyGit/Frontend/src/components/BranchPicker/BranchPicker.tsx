import { Combobox } from "@base-ui/react/combobox";
import { useMemo, useState } from "react";
import {
	ChevronDown,
	Cloud,
	GitBranch,
	Search,
} from "@/components/icons/lovelyIcons";
import { cn } from "@/lib/utils";
import { VirtualBranchPickerList } from "./VirtualBranchPickerList";

type BranchPickerProps = {
	ariaLabel: string;
	disabled?: boolean;
	emptyMessage?: string;
	kind?: "local" | "remote";
	onValueChange: (value: string) => void;
	options: string[];
	placeholder: string;
	value: string;
};

export function BranchPicker({
	ariaLabel,
	disabled = false,
	emptyMessage = "No branches match this filter.",
	kind = "local",
	onValueChange,
	options,
	placeholder,
	value,
}: BranchPickerProps) {
	const [activeBranch, setActiveBranch] = useState<string>();
	const [query, setQuery] = useState("");
	const filteredOptions = useMemo(() => {
		const normalized = query.trim().toLocaleLowerCase();
		return normalized
			? options.filter((option) =>
					option.toLocaleLowerCase().includes(normalized),
				)
			: options;
	}, [options, query]);
	const RefIcon = kind === "remote" ? Cloud : GitBranch;

	return (
		<Combobox.Root
			disabled={disabled}
			filteredItems={filteredOptions}
			inputValue={query}
			items={options}
			onInputValueChange={(nextQuery) => setQuery(nextQuery)}
			onItemHighlighted={(branch) => setActiveBranch(branch)}
			onOpenChange={(open) => {
				setQuery("");
				setActiveBranch(open ? value || undefined : undefined);
			}}
			onValueChange={(nextValue) => {
				if (nextValue) onValueChange(nextValue);
			}}
			value={value || null}
			virtualized
		>
			<Combobox.Trigger
				aria-label={ariaLabel}
				className="group flex h-8 w-full items-center justify-between gap-2 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-input/30 dark:hover:bg-input/50"
			>
				<span className="flex min-w-0 items-center gap-2">
					<RefIcon
						aria-hidden="true"
						className="size-4 shrink-0 text-muted-foreground"
					/>
					<span className={cn("truncate", !value && "text-muted-foreground")}>
						{value || placeholder}
					</span>
				</span>
				<ChevronDown
					aria-hidden="true"
					className="size-4 shrink-0 text-muted-foreground transition-transform group-data-[popup-open]:rotate-180"
				/>
			</Combobox.Trigger>
			<Combobox.Portal>
				<Combobox.Positioner
					align="start"
					className="isolate z-50"
					positionMethod="fixed"
					sideOffset={4}
				>
					<Combobox.Popup
						className="w-(--anchor-width) min-w-64 origin-(--transform-origin) rounded-lg bg-popover text-popover-foreground shadow-md ring-1 ring-foreground/10 duration-100 data-[side=bottom]:slide-in-from-top-2 data-[side=top]:slide-in-from-bottom-2 data-open:animate-in data-open:fade-in-0 data-open:zoom-in-95 data-closed:animate-out data-closed:fade-out-0 data-closed:zoom-out-95"
						initialFocus
					>
						<div className="relative border-b p-2">
							<Search
								aria-hidden="true"
								className="pointer-events-none absolute left-4 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
							/>
							<Combobox.Input
								aria-label={`Filter ${ariaLabel.toLocaleLowerCase()}`}
								className="h-8 w-full rounded-md border border-input bg-transparent pr-2 pl-8 text-sm outline-none placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
								onInput={(event) => setQuery(event.currentTarget.value)}
								placeholder="Filter branches"
							/>
						</div>
						{filteredOptions.length > 0 ? (
							<VirtualBranchPickerList
								activeBranch={activeBranch}
								kind={kind}
								onActiveBranchChange={setActiveBranch}
								options={filteredOptions}
								selected={value}
							/>
						) : (
							<p className="px-3 py-4 text-sm text-muted-foreground">
								{emptyMessage}
							</p>
						)}
					</Combobox.Popup>
				</Combobox.Positioner>
			</Combobox.Portal>
		</Combobox.Root>
	);
}
