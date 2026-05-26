/** biome-ignore-all lint/suspicious/noArrayIndexKey: shimmer has no id */
import type { CommitGraphRow } from "../types/graph";
import { AuthorCell } from "./AuthorCell";
import { CommitMessage } from "./CommitMessage";
import { GraphCell } from "./GraphCell";
import { HashCell } from "./HashCell";
import { RefCell } from "./RefCell";
import { SkeletonShimmer } from "./SkeletonShimmer";

export function CommitRow({
	graphContentWidth,
	graphScrollLeft,
	row,
	rowIndex,
	templateColumns,
}: {
	graphContentWidth: number;
	graphScrollLeft: number;
	row: CommitGraphRow | null;
	rowIndex: number;
	templateColumns: string;
}) {
	if (!row) {
		return (
			<div
				className={`relative grid h-[22px] overflow-hidden leading-[22px] ${rowIndex % 2 === 0 ? "bg-background dark:bg-background" : "bg-card/70 dark:bg-card/45"} hover:bg-accent/75 dark:hover:bg-accent/60`}
				style={{ gridTemplateColumns: templateColumns }}
			>
				<div className="min-w-0 overflow-hidden border-r px-[6px] py-[2px]">
					<SkeletonShimmer className="mt-[7px] block h-2 w-20 rounded-full" />
				</div>
				<div className="min-w-0 overflow-hidden border-r bg-card/60 px-2">
					<div className="flex h-full items-center gap-2">
						<SkeletonShimmer className="block h-1 w-8 rounded-full" />
						<SkeletonShimmer className="block h-1 w-10 rounded-full" />
						<SkeletonShimmer className="block h-[6px] w-[6px] rounded-full" />
					</div>
				</div>
				<div className="min-w-0 overflow-hidden border-r px-2">
					<SkeletonShimmer className="mt-[7px] block h-2 w-[240px] max-w-full rounded-full" />
				</div>
				<div className="min-w-0 overflow-hidden border-r px-2">
					<SkeletonShimmer className="mt-[7px] block h-2 w-14 rounded-full" />
				</div>
				<div className="min-w-0 overflow-hidden px-2">
					<SkeletonShimmer className="mt-[7px] block h-2 w-28 rounded-full" />
				</div>
			</div>
		);
	}

	return (
		<div
			className={`grid h-[22px] leading-[22px] ${rowIndex % 2 === 0 ? "bg-background dark:bg-background" : "bg-card/70 dark:bg-card/45"} hover:bg-accent/75 dark:hover:bg-accent/60`}
			style={{ gridTemplateColumns: templateColumns }}
		>
			<div className="min-w-0 overflow-hidden border-r px-[6px] py-[2px]">
				<RefCell row={row} />
			</div>
			<div className="min-w-0 overflow-hidden border-r bg-card/60">
				<GraphCell
					graphContentWidth={graphContentWidth}
					graphScrollLeft={graphScrollLeft}
					row={row}
				/>
			</div>
			<div className="min-w-0 overflow-hidden border-r px-2">
				<CommitMessage row={row} />
			</div>
			<div className="min-w-0 overflow-hidden border-r px-2">
				<HashCell row={row} />
			</div>
			<div className="min-w-0 overflow-hidden px-2">
				<AuthorCell row={row} />
			</div>
		</div>
	);
}
