import { useVirtualizer } from "@tanstack/react-virtual";
import { useRef } from "react";
import type { RefsAccordionActions } from "./RefsAccordion";
import type { RefPanelSection } from "./RefsPanelData";
import { RefPanelRow } from "./RefsPanelSections";

export function VirtualRefSection({
	actions,
	section,
}: {
	actions: RefsAccordionActions;
	section: RefPanelSection;
}) {
	const scrollRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: section.items.length,
		estimateSize: () => 32,
		getScrollElement: () => scrollRef.current,
		overscan: 6,
	});
	return (
		<div className="custom-scrollbar h-full overflow-y-auto" ref={scrollRef}>
			<div className="relative" style={{ height: virtualizer.getTotalSize() }}>
				{virtualizer.getVirtualItems().map((virtualRow) => {
					const item = section.items[virtualRow.index];
					return item ? (
						<div
							className="absolute inset-x-0"
							key={`${item.kind}:${item.name}:${item.commitHash}`}
							style={{ transform: `translateY(${virtualRow.start}px)` }}
						>
							<RefPanelRow {...actions} item={item} />
						</div>
					) : null;
				})}
			</div>
		</div>
	);
}
