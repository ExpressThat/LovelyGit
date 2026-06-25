import {
	Tooltip,
	TooltipContent,
	TooltipTrigger,
} from "@/components/ui/tooltip";
import type { CommitGraphRow } from "@/generated/types";
import {
	buildLegacyRefs,
	groupRefs,
	normalizeRefs,
	RefIcon,
	refLabelForRemotes,
	type RefGroup,
	uniqueKinds,
} from "./RefCellUtils";

export function RefCell({
	currentBranchName,
	remotePrefixes,
	row,
}: {
	currentBranchName: string | null;
	remotePrefixes: string[];
	row: CommitGraphRow;
}) {
	const refs =
		row.commit.refs.length > 0
			? normalizeRefs(row.commit.refs, row.commit.tags, remotePrefixes)
			: buildLegacyRefs(row.commit.branches, row.commit.tags, remotePrefixes);
	const groups = groupRefs(refs, remotePrefixes, currentBranchName);

	if (groups.length === 0) {
		return <div className="h-[17px]" />;
	}

	const primaryGroup = groups[0];
	return (
		<div className="flex min-w-0 gap-1">
			<RefGroupPill
				group={primaryGroup}
				hiddenGroups={groups.slice(1)}
				remotePrefixes={remotePrefixes}
			/>
		</div>
	);
}
function RefGroupPill({
	group,
	hiddenGroups,
	remotePrefixes,
}: {
	group: RefGroup;
	hiddenGroups: RefGroup[];
	remotePrefixes: string[];
}) {
	const hiddenCount = hiddenGroups.length;
	const icons = uniqueKinds(group.refs);
	const pill = (
		<span className="inline-flex h-[17px] max-w-full items-center gap-1 overflow-hidden whitespace-nowrap rounded-[3px] border border-border bg-secondary px-1 text-[11px] text-secondary-foreground shadow-sm">
			<span className="inline-flex shrink-0 items-center gap-0.5">
				{icons.map((kind) => (
					<RefIcon kind={kind} key={kind} />
				))}
			</span>
			<span className="min-w-0 flex-1 truncate">
				{refLabelForRemotes(group.primary.name, remotePrefixes)}
			</span>
			{hiddenCount > 0 ? (
				<span className="shrink-0 font-semibold text-muted-foreground">
					+{hiddenCount}
				</span>
			) : null}
		</span>
	);

	if (hiddenGroups.length === 0) {
		return pill;
	}

	return (
		<Tooltip>
			<TooltipTrigger
				closeDelay={100}
				delay={150}
				render={<span className="inline-flex min-w-0 max-w-full" />}
			>
				{pill}
			</TooltipTrigger>
			<TooltipContent
				align="start"
				className="flex min-w-max flex-col gap-1 p-1"
				side="bottom"
				sideOffset={4}
			>
				{hiddenGroups.map((refGroup) => (
					<RefListGroupPill
						group={refGroup}
						key={refGroup.key}
						remotePrefixes={remotePrefixes}
					/>
				))}
			</TooltipContent>
		</Tooltip>
	);
}

function RefListGroupPill({
	group,
	remotePrefixes,
}: {
	group: RefGroup;
	remotePrefixes: string[];
}) {
	const icons = uniqueKinds(group.refs);
	return (
		<span className="inline-flex h-6 max-w-[420px] items-center gap-1 rounded-sm border border-border bg-secondary px-2 text-xs text-secondary-foreground shadow-sm">
			<span className="inline-flex shrink-0 items-center gap-0.5">
				{icons.map((kind) => (
					<RefIcon kind={kind} key={kind} />
				))}
			</span>
			<span className="truncate">
				{refLabelForRemotes(group.primary.name, remotePrefixes)}
			</span>
		</span>
	);
}
