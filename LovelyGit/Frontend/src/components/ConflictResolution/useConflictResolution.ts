import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { toast } from "sonner";
import type {
	ConflictResolutionSource,
	WorkingTreeChangedFile,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { useSetting } from "@/lib/settings/settingsStore";
import {
	areConflictChoicesResolved,
	type ConflictChoice,
	createConflictChoices,
	hasConflictMarkers,
	renderConflictResult,
} from "./conflictDocument";
import {
	conflictErrorMessage,
	requiresWholeFileChoice,
} from "./conflictResolutionUtils";
import {
	ConflictDocumentCache,
	type ConflictLoadState,
	ConflictResolutionVariantCache,
} from "./conflictResolutionVariantCache";
import { prepareConflictResultPayload } from "./conflictResultPayload";
import { loadConflictTextPayloads } from "./conflictTextPayload";
import { verifyExternalConflictResolved } from "./externalConflictVerification";

type BusyAction = "save" | "external" | null;

export function useConflictResolution({
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
	const ignoreWhitespace = useSetting("CommitDiffIgnoreWhitespace");
	const wrapLines = useSetting("CommitDiffWrapLines");
	const contextLines = useSetting("CommitDiffContextLines");
	const lineDisplayMode = useSetting("CommitDiffLineDisplayMode");
	const [state, setState] = useState<ConflictLoadState>({ status: "loading" });
	const [choices, setChoices] = useState<Record<number, ConflictChoice>>({});
	const [resultText, setResultText] = useState("");
	const [isManualResult, setIsManualResult] = useState(false);
	const [wholeFileSelection, setWholeFileSelection] = useState<
		ConflictResolutionSource | "delete" | null
	>(null);
	const [busyAction, setBusyAction] = useState<BusyAction>(null);
	const [actionError, setActionError] = useState<string | null>(null);
	const [activeConflict, setActiveConflict] = useState(0);
	const loadedIdentity = useRef<string | null>(null);
	const documentCache = useRef(new ConflictDocumentCache());
	const variantCache = useRef(new ConflictResolutionVariantCache());
	const fetchConflict = useCallback(
		() =>
			variantCache.current.load(
				`${repositoryId}\0${file.path}`,
				ignoreWhitespace,
				async (sibling) => {
					const conflict = await sendRequestWithResponse({
						commandType: "GetConflictResolution",
						arguments: {
							repositoryId,
							path: file.path,
							viewMode: "SideBySide",
							ignoreWhitespace,
						},
					});
					return conflict ? loadConflictTextPayloads(conflict, sibling) : null;
				},
			),
		[file.path, ignoreWhitespace, repositoryId],
	);

	useEffect(() => {
		let active = true;
		if (loadedIdentity.current === null) setState({ status: "loading" });
		fetchConflict()
			.then((conflict) => {
				if (!active || !conflict) return;
				const owner = `${repositoryId}\0${file.path}`;
				const identity = `${owner}\0${conflict.worktreeFingerprint}`;
				const preparedDocument = documentCache.current.get(owner, conflict);
				if (loadedIdentity.current === identity) {
					setState({ status: "loaded", conflict, document: preparedDocument });
					return;
				}
				loadedIdentity.current = identity;
				const initialChoices = createConflictChoices(preparedDocument);
				setChoices(initialChoices);
				setResultText(renderConflictResult(preparedDocument, initialChoices));
				setIsManualResult(false);
				setWholeFileSelection(null);
				setActiveConflict(0);
				setState({ status: "loaded", conflict, document: preparedDocument });
			})
			.catch((error: unknown) => {
				if (active)
					setState({ status: "error", message: conflictErrorMessage(error) });
			});
		return () => {
			active = false;
		};
	}, [fetchConflict, file.path, repositoryId]);

	const document = state.status === "loaded" ? state.document : [];
	const textConflicts = document.filter(
		(segment) => segment.kind === "conflict",
	);
	const wholeFile =
		state.status === "loaded" && requiresWholeFileChoice(state.conflict);
	const resultHasConflictMarkers = useMemo(
		() => hasConflictMarkers(resultText),
		[resultText],
	);
	const isTextResolved =
		!resultHasConflictMarkers &&
		(isManualResult || areConflictChoicesResolved(document, choices));
	const canSave = wholeFile ? wholeFileSelection !== null : isTextResolved;
	const unresolved = isManualResult
		? 0
		: textConflicts.filter(
				(segment) => choices[segment.id]?.resolution === "unresolved",
			).length;

	const updateChoice = (id: number, choice: ConflictChoice) => {
		if (isManualResult) return;
		const next = { ...choices, [id]: choice };
		setChoices(next);
		setResultText(renderConflictResult(document, next));
	};
	const resetResult = () => {
		const initial = createConflictChoices(document);
		setChoices(initial);
		setResultText(renderConflictResult(document, initial));
		setIsManualResult(false);
		setActionError(null);
	};
	const omitActiveConflict = () => {
		const choice = choices[activeConflict];
		if (!choice || isManualResult) return;
		updateChoice(activeConflict, {
			resolution: "omit",
			ours: { accepted: false, lines: choice.ours.lines.map(() => false) },
			theirs: { accepted: false, lines: choice.theirs.lines.map(() => false) },
		});
	};

	const save = async () => {
		if (state.status !== "loaded" || !canSave) return;
		setBusyAction("save");
		setActionError(null);
		try {
			const resultPayload = await prepareConflictResultPayload(
				wholeFile ? null : resultText,
			);
			await sendRequestWithResponse(
				{
					commandType: "ResolveConflict",
					arguments: {
						repositoryId,
						path: file.path,
						expectedFingerprint: state.conflict.worktreeFingerprint,
						...resultPayload,
						source:
							wholeFile && wholeFileSelection !== "delete"
								? wholeFileSelection
								: null,
						deleteResult: wholeFile && wholeFileSelection === "delete",
					},
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			await onChange?.();
			toast.success(`Resolved and staged ${file.path}`);
			onClose();
		} catch (error) {
			setActionError(conflictErrorMessage(error));
		} finally {
			setBusyAction(null);
		}
	};

	const openExternalTool = async () => {
		if (state.status !== "loaded") return;
		setBusyAction("external");
		setActionError(null);
		try {
			await sendRequestWithResponse(
				{
					commandType: "OpenConflictInMergeTool",
					arguments: { repositoryId, path: file.path },
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			await verifyExternalConflictResolved(repositoryId, file.path);
			await onChange?.();
			toast.success(`Resolved ${file.path} in the external merge tool`);
			onClose();
		} catch (error) {
			setActionError(conflictErrorMessage(error));
		} finally {
			setBusyAction(null);
		}
	};

	const editResult = (value: string) => {
		setResultText(value);
		setIsManualResult(true);
	};
	return {
		actionError,
		activeConflict,
		busyAction,
		canSave,
		choices,
		contextLines,
		editResult,
		isManualResult,
		isTextResolved,
		lineDisplayMode,
		omitActiveConflict,
		openExternalTool,
		resetResult,
		resultText,
		save,
		setActiveConflict,
		setWholeFileSelection,
		state,
		textConflicts,
		unresolved,
		updateChoice,
		wholeFile,
		wholeFileSelection,
		wrapLines,
	};
}
