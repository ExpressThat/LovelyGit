import { Archive, ArchiveRestore, PackageOpen, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { RepositoryStashItem, StashAction } from "@/generated/types";

export function StashList({
	busyAction,
	isLoading,
	loadError,
	onApply,
	onDrop,
	onPop,
	stashes,
}: {
	busyAction: StashAction | null;
	isLoading: boolean;
	loadError: string | null;
	onApply: (stash: RepositoryStashItem) => void;
	onDrop: (stash: RepositoryStashItem) => void;
	onPop: (stash: RepositoryStashItem) => void;
	stashes: RepositoryStashItem[];
}) {
	if (isLoading) return <StashLoading />;
	if (loadError) {
		return (
			<p className="rounded-md border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">
				{loadError}
			</p>
		);
	}
	if (stashes.length === 0) return <StashEmpty />;
	return (
		<div className="grid gap-2">
			{stashes.map((stash) => (
				<StashRow
					busyAction={busyAction}
					key={`${stash.selector}:${stash.commitHash}`}
					onApply={() => onApply(stash)}
					onDrop={() => onDrop(stash)}
					onPop={() => onPop(stash)}
					stash={stash}
				/>
			))}
		</div>
	);
}

function StashRow({
	busyAction,
	onApply,
	onDrop,
	onPop,
	stash,
}: {
	busyAction: StashAction | null;
	onApply: () => void;
	onDrop: () => void;
	onPop: () => void;
	stash: RepositoryStashItem;
}) {
	const isBusy = busyAction !== null;
	return (
		<article className="grid gap-2 rounded-lg border bg-card p-3">
			<div className="flex min-w-0 items-start gap-3">
				<div className="mt-0.5 rounded-md bg-muted p-1.5 text-muted-foreground">
					<PackageOpen aria-hidden="true" className="size-4" />
				</div>
				<div className="min-w-0 flex-1">
					<div className="flex items-center gap-2">
						<span className="font-mono text-xs text-primary">
							{stash.selector}
						</span>
						<span className="font-mono text-[10px] text-muted-foreground">
							{stash.commitHash.slice(0, 7)}
						</span>
					</div>
					<p className="mt-1 break-words text-sm">
						{stash.message || "Stashed working changes"}
					</p>
					{stash.createdAtUnixSeconds ? (
						<time className="mt-1 block text-xs text-muted-foreground">
							{formatStashDate(stash.createdAtUnixSeconds)}
						</time>
					) : null}
				</div>
			</div>
			<div className="flex justify-end gap-1">
				<Button disabled={isBusy} onClick={onApply} size="sm" variant="ghost">
					<ArchiveRestore aria-hidden="true" /> Apply
				</Button>
				<Button disabled={isBusy} onClick={onPop} size="sm" variant="ghost">
					<PackageOpen aria-hidden="true" /> Pop
				</Button>
				<Button
					aria-label={`Delete ${stash.selector}`}
					disabled={isBusy}
					onClick={onDrop}
					size="icon-sm"
					title={`Delete ${stash.selector}`}
					variant="ghost"
				>
					<Trash2 aria-hidden="true" />
				</Button>
			</div>
		</article>
	);
}

function StashLoading() {
	return (
		<div aria-label="Loading stashes" className="grid gap-2" role="status">
			<div className="h-24 animate-pulse rounded-lg bg-muted" />
			<div className="h-24 animate-pulse rounded-lg bg-muted" />
		</div>
	);
}

function StashEmpty() {
	return (
		<div className="grid place-items-center rounded-lg border border-dashed px-4 py-8 text-center">
			<Archive
				aria-hidden="true"
				className="mb-2 size-6 text-muted-foreground"
			/>
			<p className="font-medium">No saved stashes</p>
			<p className="mt-1 max-w-64 text-xs text-muted-foreground">
				Create one above when you need to change context without committing.
			</p>
		</div>
	);
}

function formatStashDate(value: number) {
	const date = new Date(value * 1000);
	return Number.isNaN(date.getTime())
		? "Unknown date"
		: new Intl.DateTimeFormat(undefined, {
				dateStyle: "medium",
				timeStyle: "short",
			}).format(date);
}
