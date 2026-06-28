import { GitBranch, HardDrive, LockKeyhole } from "lucide-react";
import type { RepositoryWorktreeItem } from "@/generated/types";

export function WorktreeSection({
	query,
	worktrees,
}: {
	query: string;
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
				<span>{filtered.length}</span>
			</div>
			<div className="grid gap-1">
				{filtered.map((worktree) => (
					<WorktreeRow key={worktree.path} worktree={worktree} />
				))}
			</div>
		</section>
	);
}

function WorktreeRow({ worktree }: { worktree: RepositoryWorktreeItem }) {
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

	return (
		<div
			aria-current={worktree.isCurrent ? "true" : undefined}
			className={`flex min-w-0 items-center gap-2 rounded-lg px-2 py-1.5 text-sm ${
				worktree.isCurrent
					? "bg-secondary text-secondary-foreground"
					: "text-sidebar-foreground"
			}`}
			title={title}
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
		</div>
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
