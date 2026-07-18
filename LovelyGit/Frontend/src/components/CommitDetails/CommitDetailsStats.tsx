import type { CommitStats } from "@/generated/types";

export function CommitDetailsStats({
	fileCount,
	hasLineStats,
	stats,
}: {
	fileCount: number;
	hasLineStats: boolean;
	stats: CommitStats;
}) {
	return (
		<>
			<section className="grid grid-cols-3 gap-2">
				<Stat label="Files" value={fileCount} />
				<Stat
					label="Additions"
					value={
						hasLineStats ? `+${stats.additions.toLocaleString()}` : "Deferred"
					}
				/>
				<Stat
					label="Deletions"
					value={
						hasLineStats ? `-${stats.deletions.toLocaleString()}` : "Deferred"
					}
				/>
			</section>
			{hasLineStats ? null : (
				<p className="text-xs text-muted-foreground">
					Line totals are deferred for very large commits. Open a file for its
					exact diff.
				</p>
			)}
		</>
	);
}

function Stat({ label, value }: { label: string; value: number | string }) {
	return (
		<div className="rounded-md border bg-card p-2">
			<div className="text-[10px] font-semibold uppercase text-muted-foreground">
				{label}
			</div>
			<div className="mt-1 font-mono text-sm text-foreground">{value}</div>
		</div>
	);
}
