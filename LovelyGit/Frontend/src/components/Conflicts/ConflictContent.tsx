import { type UIEvent, useEffect, useMemo, useRef, useState } from "react";
import type { GitConflictFileContentResponse } from "@/generated/types";
import { ConflictChoicePane } from "./ConflictChoicePane";
import {
	type ConflictChoice,
	composeConflictResult,
	composeConflictResultLines,
	parseConflictHunks,
	textFromConflictLines,
} from "./ConflictHunks";
import { ConflictResultEditor } from "./ConflictResultEditor";
import { copyScrollPosition } from "./ConflictScrollSync";

export type ContentState =
	| { status: "idle"; content: null; message?: string }
	| { status: "loading"; content: null }
	| { status: "loaded"; content: GitConflictFileContentResponse }
	| { status: "error"; content: null; message: string };

export function ConflictContent({
	onResultTextChange,
	oursLabel,
	resultText,
	state,
	theirsLabel,
}: {
	onResultTextChange: (value: string) => void;
	oursLabel: string;
	resultText: string;
	state: ContentState;
	theirsLabel: string;
}) {
	if (state.status === "loading") {
		return (
			<div className="p-4 text-muted-foreground text-sm">Loading file.</div>
		);
	}
	if (state.status === "error") {
		return <div className="p-4 text-destructive text-sm">{state.message}</div>;
	}
	if (state.status !== "loaded") {
		return (
			<div className="p-4 text-muted-foreground text-sm">No file selected.</div>
		);
	}
	if (state.content.isBinary) {
		return (
			<div className="p-4 text-muted-foreground text-sm">Binary conflict.</div>
		);
	}
	return (
		<LoadedConflictContent
			content={state.content}
			onResultTextChange={onResultTextChange}
			oursLabel={oursLabel}
			resultText={resultText}
			theirsLabel={theirsLabel}
		/>
	);
}

function LoadedConflictContent({
	content,
	onResultTextChange,
	oursLabel,
	resultText,
	theirsLabel,
}: {
	content: GitConflictFileContentResponse;
	onResultTextChange: (value: string) => void;
	oursLabel: string;
	resultText: string;
	theirsLabel: string;
}) {
	const originalResultText = textFromConflictLines(content.resultLines);
	const hunks = useMemo(
		() => parseConflictHunks(originalResultText),
		[originalResultText],
	);
	const [choices, setChoices] = useState<ConflictChoice[]>([]);
	const [activeHunkIndex, setActiveHunkIndex] = useState(0);
	const currentPaneScrollRef = useRef<HTMLElement | null>(null);
	const incomingPaneScrollRef = useRef<HTMLElement | null>(null);
	const resultPaneScrollRef = useRef<HTMLElement | null>(null);
	const isSyncingScrollRef = useRef(false);
	const syncingSourceRef = useRef<HTMLElement | null>(null);

	useEffect(() => {
		const defaultChoices = hunks.map(() => "current" as const);
		setChoices(defaultChoices);
		setActiveHunkIndex(0);
		onResultTextChange(
			composeConflictResult(originalResultText, hunks, defaultChoices),
		);
	}, [hunks, onResultTextChange, originalResultText]);

	const chooseHunk = (index: number, choice: Exclude<ConflictChoice, null>) => {
		const nextChoices = hunks.map((_, choiceIndex) =>
			choiceIndex === index ? choice : (choices[choiceIndex] ?? null),
		);
		setChoices(nextChoices);
		setActiveHunkIndex(Math.min(index + 1, Math.max(0, hunks.length - 1)));
		onResultTextChange(
			composeConflictResult(originalResultText, hunks, nextChoices),
		);
	};
	const resultLines = composeConflictResultLines(
		content.resultLines,
		content.oursLines,
		content.theirsLines,
		hunks,
		choices,
	);
	const syncChoicePaneScroll =
		(source: "current" | "incoming" | "result") =>
		(event: UIEvent<HTMLElement>) => {
			if (
				isSyncingScrollRef.current &&
				syncingSourceRef.current !== event.currentTarget
			) {
				return;
			}
			const targets = [
				source === "current" ? null : currentPaneScrollRef.current,
				source === "incoming" ? null : incomingPaneScrollRef.current,
				source === "result" ? null : resultPaneScrollRef.current,
			];
			isSyncingScrollRef.current = true;
			syncingSourceRef.current = event.currentTarget;
			for (const target of targets) {
				copyScrollPosition(event.currentTarget, target);
			}
			window.requestAnimationFrame(() => {
				isSyncingScrollRef.current = false;
				syncingSourceRef.current = null;
			});
		};

	return (
		<div className="flex min-h-0 flex-1 flex-col overflow-hidden">
			<div className="grid min-h-0 flex-1 grid-cols-1 overflow-hidden xl:grid-cols-2">
				<ConflictChoicePane
					activeHunkIndex={activeHunkIndex}
					choices={choices}
					hunks={hunks}
					lines={content.oursLines}
					onChoose={chooseHunk}
					onScroll={syncChoicePaneScroll("current")}
					scrollContainerRef={currentPaneScrollRef}
					side="current"
					title={oursLabel}
				/>
				<ConflictChoicePane
					activeHunkIndex={activeHunkIndex}
					choices={choices}
					hunks={hunks}
					lines={content.theirsLines}
					onChoose={chooseHunk}
					onScroll={syncChoicePaneScroll("incoming")}
					scrollContainerRef={incomingPaneScrollRef}
					side="incoming"
					title={theirsLabel}
				/>
			</div>
			<ConflictResultEditor
				activeIndex={activeHunkIndex}
				hunkCount={hunks.length}
				onChange={onResultTextChange}
				onNext={() =>
					setActiveHunkIndex((index) => Math.min(hunks.length - 1, index + 1))
				}
				onPrevious={() => setActiveHunkIndex((index) => Math.max(0, index - 1))}
				onScroll={syncChoicePaneScroll("result")}
				scrollContainerRef={resultPaneScrollRef}
				lines={resultLines}
				value={resultText}
			/>
		</div>
	);
}
