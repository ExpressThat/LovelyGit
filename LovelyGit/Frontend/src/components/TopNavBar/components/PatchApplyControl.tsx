import { FileInput, LoaderCircle } from "lucide-react";
import { lazy, Suspense } from "react";
import { SurfaceLoading } from "@/AppLazySurfaces";
import { usePatchApply } from "./usePatchApply";

const PatchApplyDialog = lazy(() =>
	import("./PatchApplyDialog").then((module) => ({
		default: module.PatchApplyDialog,
	})),
);

export function PatchApplyControl({
	onApplied,
	repositoryId,
}: {
	onApplied: () => void;
	repositoryId: string | null;
}) {
	const patch = usePatchApply(repositoryId, onApplied);
	return (
		<>
			<button
				aria-label="Apply patch"
				className="inline-flex h-9 items-center gap-2 rounded-md px-3 font-medium text-sm hover:bg-accent disabled:pointer-events-none disabled:opacity-40"
				disabled={!repositoryId || patch.isSelecting || patch.isApplying}
				onClick={() => void patch.choosePatch()}
				title="Choose and apply a patch file"
				type="button"
			>
				{patch.isSelecting ? (
					<LoaderCircle aria-hidden="true" className="size-4 animate-spin" />
				) : (
					<FileInput aria-hidden="true" className="size-4" />
				)}
				<span>{patch.isSelecting ? "Reading patch…" : "Apply patch"}</span>
			</button>
			{patch.preview ? (
				<Suspense
					fallback={<SurfaceLoading label="Opening patch preview" overlay />}
				>
					<PatchApplyDialog
						isApplying={patch.isApplying}
						onApply={() => void patch.applyPatch()}
						onOpenChange={(open) => {
							if (!open && !patch.isApplying) patch.setPreview(null);
						}}
						onReverseChange={patch.setReverse}
						onStageChangesChange={patch.setStageChanges}
						preview={patch.preview}
						reverse={patch.reverse}
						stageChanges={patch.stageChanges}
					/>
				</Suspense>
			) : null}
		</>
	);
}
