import { AlertTriangle } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import type {
	ConflictResolutionResponse,
	ConflictResolutionSource,
	WorkingTreeChangedFile,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { useSetting } from "@/lib/settings/settingsStore";
import { DiffContent, LoadingDiff } from "../CommitFileDiff/DiffContent";
import { WorkingTreeDiffHeader } from "../WorkingChanges/WorkingTreeDiffHeader";
import { ConflictResultPanel } from "./ConflictResultPanel";
import {
	createConflictChoices,
	hasConflictMarkers,
	parseConflictDocument,
	renderConflictResult,
} from "./conflictDocument";
import { WholeFileResolutionPanel } from "./WholeFileResolutionPanel";

type LoadState =
	| { status: "loading" }
	| { status: "error"; message: string }
	| { status: "loaded"; conflict: ConflictResolutionResponse };

export function ConflictResolutionView({
	file,
	onChange,
	onClose,
	repositoryId,
}: {
	file: WorkingTreeChangedFile;
	onChange?: () => Promise<void> | void;
	onClose: () => void;
	repositoryId: string;
}) {
	const viewMode = useSetting("CommitDiffViewMode");
	const contextLines = useSetting("CommitDiffContextLines");
	const lineDisplayMode = useSetting("CommitDiffLineDisplayMode");
	const wrapLines = useSetting("CommitDiffWrapLines");
	const ignoreWhitespace = useSetting("CommitDiffIgnoreWhitespace");
	const [state, setState] = useState<LoadState>({ status: "loading" });
	const [choices, setChoices] = useState<
		ReturnType<typeof createConflictChoices>
	>({});
	const [resultText, setResultText] = useState("");
	const [wholeFileSelection, setWholeFileSelection] = useState<
		ConflictResolutionSource | "delete" | null
	>(null);
	const [isBusy, setIsBusy] = useState(false);
	const [actionError, setActionError] = useState<string | null>(null);

	const fetchConflict = useCallback(
		() =>
			sendRequestWithResponse({
				commandType: "GetConflictResolution",
				arguments: {
					repositoryId,
					path: file.path,
					viewMode,
					ignoreWhitespace,
				},
			}),
		[file.path, ignoreWhitespace, repositoryId, viewMode],
	);

	useEffect(() => {
		let active = true;
		setState({ status: "loading" });
		fetchConflict()
			.then((conflict) => {
				if (!active || !conflict) return;
				const nextDocument = createEditableDocument(conflict);
				const initialChoices = createConflictChoices(nextDocument);
				setChoices(initialChoices);
				setResultText(renderConflictResult(nextDocument, initialChoices));
				setWholeFileSelection(null);
				setState({ status: "loaded", conflict });
			})
			.catch((error: unknown) => {
				if (active) setState({ status: "error", message: errorMessage(error) });
			});
		return () => {
			active = false;
		};
	}, [fetchConflict]);

	const document = useMemo(
		() =>
			state.status === "loaded" ? createEditableDocument(state.conflict) : [],
		[state],
	);

	const updateChoice = (id: number, choice: (typeof choices)[number]) => {
		const next = { ...choices, [id]: choice };
		setChoices(next);
		setResultText(renderConflictResult(document, next));
	};

	const resetResult = () => {
		const initial = createConflictChoices(document);
		setChoices(initial);
		setResultText(renderConflictResult(document, initial));
	};

	const resolve = async () => {
		if (state.status !== "loaded") return;
		setIsBusy(true);
		setActionError(null);
		try {
			const isWholeFile = requiresWholeFileChoice(state.conflict);
			await sendRequestWithResponse({
				commandType: "ResolveConflict",
				arguments: {
					repositoryId,
					path: file.path,
					expectedFingerprint: state.conflict.worktreeFingerprint,
					resultText: isWholeFile ? null : resultText,
					source:
						isWholeFile && wholeFileSelection !== "delete"
							? wholeFileSelection
							: null,
					deleteResult: isWholeFile && wholeFileSelection === "delete",
				},
			});
			await onChange?.();
			toast.success(`Resolved ${file.path}`);
			onClose();
		} catch (error) {
			setActionError(errorMessage(error));
		} finally {
			setIsBusy(false);
		}
	};

	return (
		<section className="flex h-full min-w-0 flex-1 flex-col overflow-hidden border-l bg-background text-foreground">
			<WorkingTreeDiffHeader file={file} onClose={onClose} />
			{state.status === "loading" ? <LoadingDiff /> : null}
			{state.status === "error" ? (
				<ErrorMessage message={state.message} />
			) : null}
			{state.status === "loaded" ? (
				<>
					{actionError ? <ErrorMessage message={actionError} /> : null}
					<div className="flex min-h-0 flex-1 flex-col overflow-hidden">
						<div className="flex h-7 shrink-0 items-center border-b bg-muted/30 px-3 text-[10px] font-semibold uppercase tracking-wide text-muted-foreground">
							Current branch (ours) ↔ Incoming branch (theirs)
						</div>
						<div className="min-h-0 flex-1 overflow-hidden">
							{state.conflict.comparison ? (
								<DiffContent
									contextLines={contextLines}
									diff={state.conflict.comparison}
									lineDisplayMode={lineDisplayMode}
									wrapLines={wrapLines}
								/>
							) : (
								<WholeFileNotice />
							)}
						</div>
					</div>
					{requiresWholeFileChoice(state.conflict) ? (
						<WholeFileResolutionPanel
							exists={{
								base: state.conflict.base.exists,
								ours: state.conflict.ours.exists,
								theirs: state.conflict.theirs.exists,
							}}
							isBusy={isBusy}
							onResolve={resolve}
							selection={wholeFileSelection}
							setSelection={setWholeFileSelection}
						/>
					) : (
						<ConflictResultPanel
							choices={choices}
							isBusy={isBusy}
							isResolved={!hasConflictMarkers(resultText)}
							onChoice={updateChoice}
							onEdit={setResultText}
							onReset={resetResult}
							onResolve={resolve}
							segments={document}
							value={resultText}
						/>
					)}
				</>
			) : null}
		</section>
	);
}

function createEditableDocument(conflict: ConflictResolutionResponse) {
	const text = conflict.result.text ?? "";
	const parsed = parseConflictDocument(text);
	if (parsed.some((segment) => segment.kind === "conflict")) return parsed;
	const ours = withFinalNewline(conflict.ours.text ?? "");
	const theirs = withFinalNewline(conflict.theirs.text ?? "");
	return parseConflictDocument(
		`<<<<<<< current branch\n${ours}=======\n${theirs}>>>>>>> incoming branch\n`,
	);
}

function withFinalNewline(text: string) {
	return text.length > 0 && !text.endsWith("\n") ? `${text}\n` : text;
}

function requiresWholeFileChoice(conflict: ConflictResolutionResponse) {
	if (!conflict.ours.exists || !conflict.theirs.exists) return true;
	return [conflict.ours, conflict.theirs, conflict.result].some(
		(version) => version.isBinary || version.isTooLarge,
	);
}

function errorMessage(error: unknown) {
	return error instanceof Error
		? error.message
		: "Failed to resolve the conflict.";
}

function ErrorMessage({ message }: { message: string }) {
	return (
		<div className="m-4 rounded-md border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">
			{message}
		</div>
	);
}

function WholeFileNotice() {
	return (
		<div className="grid h-full place-items-center p-6 text-center text-sm text-muted-foreground">
			<div>
				<AlertTriangle className="mx-auto mb-2 size-5" />
				Line comparison is unavailable for this file.
			</div>
		</div>
	);
}
