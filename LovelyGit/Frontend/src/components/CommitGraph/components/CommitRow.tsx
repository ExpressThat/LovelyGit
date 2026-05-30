/** biome-ignore-all lint/suspicious/noArrayIndexKey: shimmer has no id */
import type { ReactNode } from "react";
import type { CommitGraphRow } from "@/generated/ExpressThat.LovelyGit.Services.Git.CommitGraph.Models";
import { AuthorCell } from "./AuthorCell";
import { CommitMessage } from "./CommitMessage";
import { GraphCell } from "./GraphCell";
import { HashCell } from "./HashCell";
import { RefCell } from "./RefCell";
import { SkeletonShimmer } from "./SkeletonShimmer";

export function CommitRow({
	graph,
	row,
	rowIndex,
	templateColumns,
}: {
	graph: {
		contentWidth: number;
		scrollLeft: number;
	};
	row: CommitGraphRow | null;
	rowIndex: number;
	templateColumns: string;
}) {
	const rowClassName = `grid h-[22px] leading-[22px] ${rowIndex % 2 === 0 ? "bg-background dark:bg-background" : "bg-card/70 dark:bg-card/45"} hover:bg-accent/75 dark:hover:bg-accent/60`;

	if (!row) {
		return (
			<div
				className={`relative overflow-hidden ${rowClassName}`}
				style={{ gridTemplateColumns: templateColumns }}
			>
				<Column className="px-[6px] py-[2px]">
					<SkeletonShimmer className="mt-[7px] block h-2 w-20 rounded-full" />
				</Column>
				<Column className="bg-card/60 px-2">
					<div className="flex h-full items-center gap-2">
						<SkeletonShimmer className="block h-1 w-8 rounded-full" />
						<SkeletonShimmer className="block h-1 w-10 rounded-full" />
						<SkeletonShimmer className="block h-[6px] w-[6px] rounded-full" />
					</div>
				</Column>
				<Column className="px-2">
					<SkeletonShimmer className="mt-[7px] block h-2 w-[240px] max-w-full rounded-full" />
				</Column>
				<Column className="px-2">
					<SkeletonShimmer className="mt-[7px] block h-2 w-14 rounded-full" />
				</Column>
				<Column className="px-2" isLast>
					<SkeletonShimmer className="mt-[7px] block h-2 w-28 rounded-full" />
				</Column>
			</div>
		);
	}

	return (
		<div
			className={rowClassName}
			style={{ gridTemplateColumns: templateColumns }}
		>
			<Column className="px-[6px] py-[2px]">
				<RefCell row={row} />
			</Column>
			<Column className="bg-card/60">
				<GraphCell
					graphContentWidth={graph.contentWidth}
					graphScrollLeft={graph.scrollLeft}
					row={row}
				/>
			</Column>
			<Column className="px-2">
				<CommitMessage row={row} />
			</Column>
			<Column className="px-2">
				<HashCell row={row} />
			</Column>
			<Column className="px-2" isLast>
				<AuthorCell row={row} />
			</Column>
		</div>
	);
}

function Column({
	children,
	className,
	isLast = false,
}: {
	children: ReactNode;
	className: string;
	isLast?: boolean;
}) {
	return (
		<div
			className={`min-w-0 overflow-hidden ${isLast ? "" : "border-r"} ${className}`}
		>
			{children}
		</div>
	);
}
