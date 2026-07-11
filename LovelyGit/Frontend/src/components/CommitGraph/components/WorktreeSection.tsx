import { GitBranch, HardDrive, LockKeyhole, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { RepositoryWorktreeItem } from "@/generated/types";
import type { WorktreeMutationController } from "../hooks/useWorktreeMutations";
import { WorktreeContextMenu } from "./WorktreeContextMenu";

export function WorktreeSection({
	query,
	controller,
	worktrees,
}: {
	query: string;
	controller: WorktreeMutationController;
	worktrees: RepositoryWorktreeItem[];
}) {
	const filtered = filterWorktrees(worktrees, query);
	if (filtered.length === 0) {
		return null;
	}

	return (
		<section className="mb-3 last:mb-0">
			<div className="mb-1 flex items-center justify-between px-1 text-[10px] font-semibold uppercase text-muted-foreground">
				<span>Worktrees</span>
				<span className="flex items-center gap-1">
					<span>{filtered.length}</span>
					<Button
						aria-label="Create worktree"
						className="size-5 rounded-md"
						disabled={controller.busyPath !== null}
						onClick={() => controller.setCreateBranchName("")}
						size="icon-xs"
						title="Create worktree"
						type="button"
						variant="ghost"
					>
						<Plus aria-hidden="true" className="size-3" />
					</Button>
				</span>
			</div>
			<WorktreeList controller={controller} worktrees={filtered} />
		</section>
	);
}

export function WorktreeList({
	controller,
	worktrees,
}: {
	controller: WorktreeMutationController;
	worktrees: RepositoryWorktreeItem[];
}) {
	return (
		<div className="grid gap-1">
			{worktrees.map((worktree) => (
				<WorktreeRow
					controller={controller}
					key={worktree.path}
					worktree={worktree}
				/>
			))}
		</div>
	);
}

function WorktreeRow({
	controller,
	worktree,
}: {
	controller: WorktreeMutationController;
	worktree: RepositoryWorktreeItem;
}) {
	const label = worktree.branchName ?? "Detached HEAD";
	const title = [
		label,
		worktree.path,
		worktree.isCurrent ? "Current worktree" : null,
		worktree.isLocked
			? `Locked${worktree.lockReason ? `: ${worktree.lockReason}` : ""}`
			: null,
	]
		.filter(Boolean)
		.join(". ");

	const row = (
		<button
			aria-label={`${label} worktree at ${worktree.path}`}
			aria-current={worktree.isCurrent ? "true" : undefined}
			className={`flex min-w-0 items-center gap-2 rounded-lg px-2 py-1.5 text-sm ${
				worktree.isCurrent
					? "bg-secondary text-secondary-foreground"
					: "text-sidebar-foreground"
			}`}
			onDoubleClick={() => {
				if (!worktree.isCurrent && controller.busyPath === null) {
					controller.manage("Open", worktree);
				}
			}}
			title={title}
			type="button"
		>
			<HardDrive
				aria-hidden="true"
				className="size-3.5 shrink-0 text-emerald-400"
			/>
			<span className="grid min-w-0 flex-1 text-left leading-tight">
				<span className="flex min-w-0 items-center gap-1">
					<GitBranch
						aria-hidden="true"
						className="size-3 shrink-0 text-muted-foreground"
					/>
					<span className="truncate">{label}</span>
				</span>
				<span className="truncate font-mono text-[10px] text-muted-foreground">
					{worktree.path}
				</span>
			</span>
			{worktree.isLocked ? (
				<LockKeyhole
					aria-label="Locked worktree"
					className="size-3.5 shrink-0 text-amber-500"
				/>
			) : null}
		</button>
	);
	return (
		<WorktreeContextMenu
			disabled={controller.busyPath !== null}
			onAction={controller.manage}
			worktree={worktree}
		>
			{row}
		</WorktreeContextMenu>
	);
}

export function filterWorktrees(
	worktrees: RepositoryWorktreeItem[],
	query: string,
): RepositoryWorktreeItem[] {
	const normalizedQuery = query.trim().toLocaleLowerCase();
	if (normalizedQuery.length === 0) {
		return worktrees;
	}

	return worktrees.filter((worktree) =>
		`${worktree.branchName ?? ""} ${worktree.path} ${worktree.lockReason}`
			.toLocaleLowerCase()
			.includes(normalizedQuery),
	);
}
