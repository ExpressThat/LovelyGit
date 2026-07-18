import { useVirtualizer } from "@tanstack/react-virtual";
import { useMemo, useRef } from "react";
import type { FileBlameResponse } from "@/generated/types";
import { FileBlameRow } from "./FileBlameRow";
import {
	buildBlameLineStarts,
	findBlameHunk,
	readBlameLine,
} from "./fileBlameLines";

const ROW_HEIGHT = 24;

export function FileBlameContent({
	onSelectCommit,
	response,
}: {
	onSelectCommit: (hash: string) => void;
	response: FileBlameResponse;
}) {
	const parentRef = useRef<HTMLDivElement>(null);
	const lineStarts = useMemo(
		() => buildBlameLineStarts(response.content, response.lineCount),
		[response.content, response.lineCount],
	);
	const virtualizer = useVirtualizer({
		count: response.lineCount,
		estimateSize: () => ROW_HEIGHT,
		getScrollElement: () => parentRef.current,
		overscan: 16,
	});

	return (
		<section
			aria-label="Blamed file contents"
			className="custom-scrollbar min-h-0 flex-1 overflow-auto bg-background"
			ref={parentRef}
		>
			<div
				className="relative min-w-max"
				style={{ height: `${virtualizer.getTotalSize()}px` }}
			>
				{virtualizer.getVirtualItems().map((virtualRow) => {
					const lineNumber = virtualRow.index + 1;
					return (
						<div
							className="absolute left-0 right-0 top-0"
							key={lineNumber}
							style={{ transform: `translateY(${virtualRow.start}px)` }}
						>
							<FileBlameRow
								hunk={findBlameHunk(response.hunks, lineNumber)}
								line={readBlameLine(
									response.content,
									lineStarts,
									virtualRow.index,
								)}
								lineNumber={lineNumber}
								onSelectCommit={onSelectCommit}
							/>
						</div>
					);
				})}
			</div>
		</section>
	);
}
