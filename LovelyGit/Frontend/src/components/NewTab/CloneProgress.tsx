import { Check, LoaderCircle } from "lucide-react";
import type { CloneRepositoryProgressNotification } from "@/generated/types";

export function CloneProgress({
	progress,
}: {
	progress: CloneRepositoryProgressNotification;
}) {
	return (
		<div className="grid gap-2 rounded-lg border bg-muted/40 p-3" role="status">
			<div className="flex items-center gap-2">
				{progress.percent === 100 ? (
					<Check aria-hidden="true" className="size-4 text-primary" />
				) : (
					<LoaderCircle
						aria-hidden="true"
						className="size-4 animate-spin text-primary"
					/>
				)}
				<span className="min-w-0 flex-1 truncate font-medium text-sm">
					{progress.stage}
				</span>
			</div>
			<CloneProgressBar label="Overall progress" percent={progress.percent} />
			<CloneProgressBar
				label={`Current phase: ${progress.stage}`}
				percent={progress.phasePercent}
			/>
			<p
				className="truncate text-muted-foreground text-xs"
				title={progress.message}
			>
				{progress.message}
			</p>
		</div>
	);
}

function CloneProgressBar({
	label,
	percent,
}: {
	label: string;
	percent: number | null;
}) {
	return (
		<div className="grid gap-1">
			<div className="flex items-center gap-2 text-muted-foreground text-xs">
				<span className="min-w-0 flex-1 truncate">{label}</span>
				<span className="shrink-0 font-mono">
					{percent == null ? "Waiting for Git…" : `${percent}%`}
				</span>
			</div>
			<div
				aria-label={label}
				aria-valuemax={100}
				aria-valuemin={0}
				aria-valuenow={percent ?? undefined}
				className="h-1.5 overflow-hidden rounded-full bg-muted"
				role="progressbar"
			>
				<div
					className={
						percent == null
							? "h-full w-1/3 animate-pulse rounded-full bg-primary"
							: "h-full rounded-full bg-primary transition-[width] duration-150"
					}
					style={percent == null ? undefined : { width: `${percent}%` }}
				/>
			</div>
		</div>
	);
}
