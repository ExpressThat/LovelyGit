import type React from "react";
import type { ColKey } from "../constants";
import { COL_ORDER } from "../constants";
import { HeaderCell } from "./HeaderCell";

const headerLabels: Record<ColKey, string> = {
	author: "Author",
	branch: "Branch",
	graph: "Graph",
	hash: "Hash",
	message: "Commit Message",
};

export function CommitGraphHeader({
	isInitialLoading,
	onResizeStart,
	templateColumns,
}: {
	isInitialLoading: boolean;
	onResizeStart: (
		leftKey: ColKey,
		event: React.PointerEvent<HTMLButtonElement>,
	) => void;
	templateColumns: string;
}) {
	return (
		<div
			className="grid h-[22px] border-b bg-card text-[10px] font-bold uppercase leading-[21px] text-muted-foreground"
			style={{ gridTemplateColumns: templateColumns }}
		>
			{COL_ORDER.map((keyName, index) => (
				<HeaderCell
					key={keyName}
					keyName={keyName}
					label={
						isInitialLoading && keyName === "message"
							? "Loading"
							: headerLabels[keyName]
					}
					onResizeStart={onResizeStart}
					showHandle={index < COL_ORDER.length - 1}
				/>
			))}
		</div>
	);
}
