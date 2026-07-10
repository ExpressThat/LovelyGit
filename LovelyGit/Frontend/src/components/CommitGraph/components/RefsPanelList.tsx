import { useVirtualizer } from "@tanstack/react-virtual";
import { useMemo, useRef } from "react";
import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import type { CommitGraphRow } from "@/generated/types";
import type { RefPanelItem, RefPanelSection } from "./RefsPanelData";
import { RefPanelRow } from "./RefsPanelSections";
import type { TagAction } from "./TagContextMenu";

type RefListRow =
	| { id: string; section: RefPanelSection; type: "header" }
	| { id: string; item: RefPanelItem; type: "item" };

export function RefsPanelList({
	onSelectCommit,
	currentBranchName,
	onIntegrateBranch,
	onTagAction,
	sections,
	tagMutationBusy,
	tagRemoteName,
}: {
	currentBranchName: string | null;
	onIntegrateBranch: (mode: BranchIntegrationMode, branchName: string) => void;
	onSelectCommit: (row: CommitGraphRow) => void;
	onTagAction: (action: TagAction, tagName: string) => void;
	sections: RefPanelSection[];
	tagMutationBusy: boolean;
	tagRemoteName: string | null;
}) {
	const parentRef = useRef<HTMLDivElement>(null);
	const rows = useMemo(() => flattenSections(sections), [sections]);
	const virtualizer = useVirtualizer({
		count: rows.length,
		estimateSize: (index) => (rows[index]?.type === "header" ? 24 : 32),
		getScrollElement: () => parentRef.current,
		overscan: 8,
	});
	const virtualRows = virtualizer.getVirtualItems();

	return (
		<div
			className="custom-scrollbar min-h-0 flex-1 overflow-y-auto p-2"
			ref={parentRef}
		>
			<div
				className="relative"
				style={{ height: `${virtualizer.getTotalSize()}px` }}
			>
				{virtualRows.map((virtualRow) => {
					const row = rows[virtualRow.index];
					if (!row) {
						return null;
					}

					return (
						<div
							className="absolute left-0 right-0"
							key={row.id}
							style={{ transform: `translateY(${virtualRow.start}px)` }}
						>
							{row.type === "header" ? (
								<RefHeader section={row.section} />
							) : (
								<RefPanelRow
									currentBranchName={currentBranchName}
									item={row.item}
									onIntegrateBranch={onIntegrateBranch}
									onSelectCommit={onSelectCommit}
									onTagAction={onTagAction}
									tagMutationBusy={tagMutationBusy}
									tagRemoteName={tagRemoteName}
								/>
							)}
						</div>
					);
				})}
			</div>
		</div>
	);
}

function RefHeader({ section }: { section: RefPanelSection }) {
	return (
		<div className="mb-1 flex h-6 items-center justify-between px-1 text-[10px] font-semibold uppercase text-muted-foreground">
			<span>{section.label}</span>
			<span>{section.count}</span>
		</div>
	);
}

function flattenSections(sections: RefPanelSection[]): RefListRow[] {
	return sections.flatMap((section) => [
		{ id: `${section.kind}:header`, section, type: "header" as const },
		...section.items.map((item) => ({
			id: `${item.kind}:${item.name}:${item.commitHash}`,
			item,
			type: "item" as const,
		})),
	]);
}
