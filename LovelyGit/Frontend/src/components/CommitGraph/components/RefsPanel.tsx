import { ChevronLeft, ChevronRight, Search, X } from "lucide-react";
import { useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import type { CommitGraphRow } from "@/generated/types";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import { shortHash } from "../utils/format";
import { RefIcon } from "./RefCellUtils";
import {
	buildRefPanelSections,
	filterRefPanelSections,
	type RefPanelSection,
} from "./RefsPanelData";

export function RefsPanel({
	currentBranchName,
	onSelectCommit,
	remotePrefixes,
	rows,
}: {
	currentBranchName: string | null;
	onSelectCommit: (row: CommitGraphRow) => void;
	remotePrefixes: string[];
	rows: Array<CommitGraphRow | null>;
}) {
	const isOpen = useSetting("CommitGraphRefsPanelOpen");
	const [query, setQuery] = useState("");
	const sections = useMemo(
		() =>
			buildRefPanelSections({
				currentBranchName,
				remotePrefixes,
				rows,
			}),
		[currentBranchName, remotePrefixes, rows],
	);
	const filteredSections = useMemo(
		() => filterRefPanelSections(sections, query),
		[sections, query],
	);

	if (!isOpen) {
		return (
			<aside className="flex w-9 shrink-0 justify-center border-r bg-sidebar py-2">
				<Button
					aria-label="Show refs panel"
					onClick={() => void setSetting("CommitGraphRefsPanelOpen", true)}
					size="icon-sm"
					title="Show refs panel"
					variant="ghost"
				>
					<ChevronRight aria-hidden="true" />
				</Button>
			</aside>
		);
	}

	return (
		<aside className="flex w-56 shrink-0 flex-col border-r bg-sidebar text-sidebar-foreground">
			<header className="flex h-[34px] items-center justify-between border-b px-2">
				<h2 className="text-xs font-semibold uppercase text-muted-foreground">
					Refs
				</h2>
				<Button
					aria-label="Hide refs panel"
					onClick={() => void setSetting("CommitGraphRefsPanelOpen", false)}
					size="icon-xs"
					title="Hide refs panel"
					variant="ghost"
				>
					<ChevronLeft aria-hidden="true" />
				</Button>
			</header>
			<div className="border-b p-2">
				<div className="relative">
					<Search
						aria-hidden="true"
						className="pointer-events-none absolute left-2 top-1/2 -translate-y-1/2 text-muted-foreground"
						size={14}
					/>
					<Input
						aria-label="Filter refs"
						className="h-7 rounded-md pl-7 pr-7 text-xs"
						onChange={(event) => setQuery(event.currentTarget.value)}
						onInput={(event) => setQuery(event.currentTarget.value)}
						placeholder="Filter refs"
						value={query}
					/>
					{query ? (
						<Button
							aria-label="Clear ref filter"
							className="absolute right-1 top-1/2 size-5 -translate-y-1/2"
							onClick={() => setQuery("")}
							size="icon-xs"
							title="Clear ref filter"
							type="button"
							variant="ghost"
						>
							<X aria-hidden="true" size={12} />
						</Button>
					) : null}
				</div>
			</div>
			<div className="custom-scrollbar min-h-0 flex-1 overflow-y-auto p-2">
				{filteredSections.length > 0 ? (
					filteredSections.map((section) => (
						<RefSection
							key={section.kind}
							onSelectCommit={onSelectCommit}
							section={section}
						/>
					))
				) : (
					<RefsEmptyState hasQuery={query.trim().length > 0} />
				)}
			</div>
		</aside>
	);
}

function RefsEmptyState({ hasQuery }: { hasQuery: boolean }) {
	return (
		<p className="px-1 py-2 text-xs text-muted-foreground">
			{hasQuery
				? "No refs match this filter."
				: "Refs appear as graph pages load."}
		</p>
	);
}

function RefSection({
	onSelectCommit,
	section,
}: {
	onSelectCommit: (row: CommitGraphRow) => void;
	section: RefPanelSection;
}) {
	return (
		<section className="mb-3 last:mb-0">
			<div className="mb-1 flex items-center justify-between px-1 text-[10px] font-semibold uppercase text-muted-foreground">
				<span>{section.label}</span>
				<span>{section.count}</span>
			</div>
			<div className="grid gap-1">
				{section.items.map((item) => (
					<Button
						className="h-7 min-w-0 justify-start gap-2 px-2 font-normal"
						key={`${item.kind}:${item.name}:${item.commitHash}`}
						onClick={() => onSelectCommit(item.row)}
						title={`${item.name} at ${shortHash(item.commitHash)}`}
						variant={item.isCurrent ? "secondary" : "ghost"}
					>
						<RefIcon kind={item.kind} />
						<span className="min-w-0 flex-1 truncate text-left">
							{item.label}
						</span>
						<span className="font-mono text-[10px] text-muted-foreground">
							{shortHash(item.commitHash)}
						</span>
					</Button>
				))}
			</div>
		</section>
	);
}
