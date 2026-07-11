import { GitCompareArrows } from "lucide-react";
import { ContextMenuItem } from "@/components/ui/context-menu";
import type { CommitGraphRow } from "@/generated/types";
import { shortHash } from "../utils/format";

export function CommitComparisonMenuItem({
	base,
	onCompare,
	onSetBase,
	row,
}: {
	base: CommitGraphRow | null;
	onCompare: (row: CommitGraphRow) => void;
	onSetBase: (row: CommitGraphRow | null) => void;
	row: CommitGraphRow;
}) {
	const hash = shortHash(row.commit.hash);
	if (!base) {
		return (
			<ContextMenuItem onClick={() => onSetBase(row)}>
				<GitCompareArrows aria-hidden="true" />
				Select {hash} as comparison base
			</ContextMenuItem>
		);
	}
	const baseHash = shortHash(base.commit.hash);
	const isBase = base.commit.hash === row.commit.hash;
	return (
		<ContextMenuItem
			onClick={() => (isBase ? onSetBase(null) : onCompare(row))}
		>
			<GitCompareArrows aria-hidden="true" />
			{isBase
				? `Clear comparison base ${baseHash}`
				: `Compare ${baseHash} with ${hash}`}
		</ContextMenuItem>
	);
}
