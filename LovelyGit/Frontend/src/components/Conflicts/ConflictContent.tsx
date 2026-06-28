import type { GitConflictFileContentResponse } from "@/generated/types";
import { ConflictCodePane } from "./ConflictCodePane";

export type ContentState =
	| { status: "idle"; content: null; message?: string }
	| { status: "loading"; content: null }
	| { status: "loaded"; content: GitConflictFileContentResponse }
	| { status: "error"; content: null; message: string };

export function ConflictContent({
	oursLabel,
	state,
	theirsLabel,
}: {
	oursLabel: string;
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
		<div className="flex min-h-0 flex-1 overflow-hidden">
			<ConflictCodePane
				lines={state.content.oursLines}
				title={oursLabel}
				tone="ours"
			/>
			<ConflictCodePane
				lines={state.content.theirsLines}
				title={theirsLabel}
				tone="theirs"
			/>
			<ConflictCodePane
				lines={state.content.resultLines}
				title="Result"
				tone="result"
			/>
		</div>
	);
}
