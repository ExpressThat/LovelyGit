import { useMemo, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { GitBranch, LoaderCircle, Tag } from "@/components/icons/lovelyIcons";
import { Input } from "@/components/ui/input";
import type { RepositoryRefItem } from "@/generated/types";

const MAX_SUGGESTIONS = 8;

export function CommitSearchScopeField({
	isLoading,
	onChange,
	refs,
	value,
}: {
	isLoading: boolean;
	onChange: (value: string) => void;
	refs: RepositoryRefItem[];
	value: string;
}) {
	const [focused, setFocused] = useState(false);
	const [activeIndex, setActiveIndex] = useState(0);
	const inputRef = useRef<HTMLInputElement>(null);
	const [anchor, setAnchor] = useState<DOMRect | null>(null);
	const suggestions = useMemo(
		() => findRefSuggestions(refs, value),
		[refs, value],
	);
	const open = focused && suggestions.length > 0;
	const selectedIndex = Math.min(
		activeIndex,
		Math.max(suggestions.length - 1, 0),
	);
	const update = (nextValue: string) => {
		setActiveIndex(0);
		onChange(nextValue);
	};
	const choose = (name: string) => {
		onChange(name);
		setFocused(false);
	};
	const focus = () => {
		setAnchor(inputRef.current?.getBoundingClientRect() ?? null);
		setFocused(true);
	};

	return (
		<label
			className="grid gap-1 text-muted-foreground text-xs"
			htmlFor="commit-search-scope"
		>
			<span className="flex items-center gap-1">
				<GitBranch aria-hidden="true" className="size-3" /> Branch or tag
				{isLoading ? (
					<LoaderCircle
						aria-label="Loading refs"
						className="size-3 animate-spin"
					/>
				) : null}
			</span>
			<Input
				aria-activedescendant={
					open ? `commit-search-ref-${selectedIndex}` : undefined
				}
				aria-autocomplete="list"
				aria-controls="commit-search-ref-suggestions"
				aria-expanded={open}
				aria-label="Limit search to branch or tag"
				className="h-8"
				id="commit-search-scope"
				onBlur={() => setFocused(false)}
				onChange={(event) => update(event.currentTarget.value)}
				onFocus={focus}
				onInput={(event) => update(event.currentTarget.value)}
				onKeyDown={(event) => {
					if (!open) return;
					if (event.key === "ArrowDown" || event.key === "ArrowUp") {
						event.preventDefault();
						const delta = event.key === "ArrowDown" ? 1 : -1;
						setActiveIndex(
							(index) =>
								(index + delta + suggestions.length) % suggestions.length,
						);
					} else if (event.key === "Enter") {
						event.preventDefault();
						const selected = suggestions[selectedIndex];
						if (selected) choose(selected.name);
					} else if (event.key === "Escape") setFocused(false);
				}}
				placeholder="main, origin/main, or v1.0"
				role="combobox"
				ref={inputRef}
				value={value}
			/>
			{open && anchor
				? createPortal(
						<RefSuggestionList
							anchor={anchor}
							onChoose={choose}
							selectedIndex={selectedIndex}
							suggestions={suggestions}
						/>,
						document.body,
					)
				: null}
		</label>
	);
}

function RefSuggestionList({
	anchor,
	onChoose,
	selectedIndex,
	suggestions,
}: {
	anchor: DOMRect;
	onChoose: (name: string) => void;
	selectedIndex: number;
	suggestions: RepositoryRefItem[];
}) {
	return (
		<div
			className="fixed z-[100] mt-1 rounded-md border bg-popover p-1 text-popover-foreground shadow-md"
			id="commit-search-ref-suggestions"
			role="listbox"
			style={{ left: anchor.left, top: anchor.bottom, width: anchor.width }}
		>
			{suggestions.map((reference, index) => (
				<button
					aria-selected={index === selectedIndex}
					className="flex h-7 w-full items-center gap-2 rounded px-2 text-left hover:bg-accent aria-selected:bg-accent"
					id={`commit-search-ref-${index}`}
					key={`${reference.kind}:${reference.name}`}
					onMouseDown={(event) => event.preventDefault()}
					onClick={() => onChoose(reference.name)}
					role="option"
					type="button"
				>
					{reference.kind === "Tag" ? (
						<Tag aria-hidden="true" />
					) : (
						<GitBranch aria-hidden="true" />
					)}
					<span className="min-w-0 flex-1 truncate">{reference.name}</span>
					<span className="text-[10px] text-muted-foreground">
						{reference.kind}
					</span>
				</button>
			))}
		</div>
	);
}

export function findRefSuggestions(refs: RepositoryRefItem[], query: string) {
	const normalized = query.trim().toLocaleLowerCase();
	return refs
		.filter((reference) => reference.kind !== "Stash")
		.filter(
			(reference) =>
				!normalized || reference.name.toLocaleLowerCase().includes(normalized),
		)
		.sort((left, right) => {
			const leftStarts = left.name.toLocaleLowerCase().startsWith(normalized);
			const rightStarts = right.name.toLocaleLowerCase().startsWith(normalized);
			return (
				Number(rightStarts) - Number(leftStarts) ||
				left.name.localeCompare(right.name)
			);
		})
		.slice(0, MAX_SUGGESTIONS);
}
