import { Archive } from "@/components/icons/lovelyIcons";
import type { RepositoryStashItem, StashAction } from "@/generated/types";
import { StashVirtualList } from "./StashVirtualList";

export function StashList({
	busyAction,
	isLoading,
	loadError,
	onApply,
	onBranch,
	onDrop,
	onInspect,
	onPop,
	stashes,
}: {
	busyAction: StashAction | null;
	isLoading: boolean;
	loadError: string | null;
	onApply: (stash: RepositoryStashItem) => void;
	onBranch: (stash: RepositoryStashItem) => void;
	onDrop: (stash: RepositoryStashItem) => void;
	onInspect: (stash: RepositoryStashItem) => void;
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
		<StashVirtualList
			busyAction={busyAction}
			onApply={onApply}
			onBranch={onBranch}
			onDrop={onDrop}
			onInspect={onInspect}
			onPop={onPop}
			stashes={stashes}
		/>
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
