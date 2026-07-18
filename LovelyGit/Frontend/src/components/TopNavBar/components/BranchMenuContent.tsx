import type { KeyboardEvent } from "react";
import { LoaderCircle, Search } from "@/components/icons/lovelyIcons";
import {
	DropdownMenuGroup,
	DropdownMenuLabel,
	DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import type { RepositoryRefItem } from "@/generated/types";
import { VirtualBranchMenuList } from "./VirtualBranchMenuList";

type BranchMenuContentProps = {
	activeIndex: number;
	branches: RepositoryRefItem[];
	busy: boolean;
	currentBranchName: string | null;
	error: string | null;
	isLoading: boolean;
	onActiveIndexChange: (index: number) => void;
	onCheckout: (branchName: string) => void;
	onQueryChange: (query: string) => void;
	query: string;
};

export function BranchMenuContent({
	activeIndex,
	branches,
	busy,
	currentBranchName,
	error,
	isLoading,
	onActiveIndexChange,
	onCheckout,
	onQueryChange,
	query,
}: BranchMenuContentProps) {
	const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
		event.stopPropagation();
		if (branches.length === 0) return;

		let nextIndex: number | null = null;
		switch (event.key) {
			case "ArrowDown":
				nextIndex = (activeIndex + 1) % branches.length;
				break;
			case "ArrowUp":
				nextIndex = (activeIndex - 1 + branches.length) % branches.length;
				break;
			case "Home":
				nextIndex = 0;
				break;
			case "End":
				nextIndex = branches.length - 1;
				break;
			case "Enter":
				event.preventDefault();
				onCheckout(branches[activeIndex]?.name ?? "");
				return;
		}
		if (nextIndex !== null) {
			event.preventDefault();
			onActiveIndexChange(nextIndex);
		}
	};

	return (
		<DropdownMenuGroup>
			<DropdownMenuLabel className="px-3 pt-3 pb-2 text-sm normal-case">
				Switch branch
			</DropdownMenuLabel>
			<div className="px-2 pb-2">
				<div className="relative">
					<Search
						aria-hidden="true"
						className="pointer-events-none absolute left-2 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
					/>
					<Input
						aria-activedescendant={
							branches.length > 0
								? `branch-switcher-item-${activeIndex}`
								: undefined
						}
						aria-controls="branch-switcher-results"
						aria-label="Filter branches"
						className="h-8 pl-8 text-sm"
						onInput={(event) => onQueryChange(event.currentTarget.value)}
						onKeyDown={handleKeyDown}
						placeholder="Filter branches"
						value={query}
					/>
				</div>
			</div>
			<DropdownMenuSeparator className="my-0" />
			{isLoading ? (
				<p className="flex items-center gap-2 px-3 py-3 text-sm text-muted-foreground">
					<LoaderCircle aria-hidden="true" className="size-4 animate-spin" />
					Loading branches…
				</p>
			) : null}
			{!isLoading && error ? (
				<p className="px-3 py-3 text-sm text-destructive">{error}</p>
			) : null}
			{!isLoading && !error && branches.length === 0 ? (
				<p className="px-3 py-3 text-sm text-muted-foreground">
					No branches match this filter.
				</p>
			) : null}
			{!isLoading && !error && branches.length > 0 ? (
				<VirtualBranchMenuList
					activeIndex={activeIndex}
					branches={branches}
					busy={busy}
					currentBranchName={currentBranchName}
					onActiveIndexChange={onActiveIndexChange}
					onCheckout={onCheckout}
				/>
			) : null}
		</DropdownMenuGroup>
	);
}
