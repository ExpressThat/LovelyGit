import { AnimatePresence, motion } from "motion/react";
import { FileDiff, LoaderCircle } from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import {
	Dialog,
	DialogClose,
	DialogContent,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import type { PatchPreviewResponse } from "@/generated/types";
import { PatchApplyPreview } from "./PatchApplyPreview";

export function PatchApplyDialog({
	isApplying,
	onApply,
	onOpenChange,
	onReverseChange,
	onStageChangesChange,
	preview,
	reverse,
	stageChanges,
}: {
	isApplying: boolean;
	onApply: () => void;
	onOpenChange: (open: boolean) => void;
	onReverseChange: (checked: boolean) => void;
	onStageChangesChange: (checked: boolean) => void;
	preview: PatchPreviewResponse | null;
	reverse: boolean;
	stageChanges: boolean;
}) {
	return (
		<Dialog open={preview !== null} onOpenChange={onOpenChange}>
			<DialogContent className="max-h-[82vh] overflow-hidden sm:max-w-2xl">
				<DialogHeader>
					<DialogTitle className="flex items-center gap-2">
						<FileDiff className="size-5 text-primary" />
						Apply patch
					</DialogTitle>
					<DialogDescription className="truncate" title={preview?.path ?? ""}>
						Review {preview?.fileName ?? "the selected patch"} before changing
						the current repository.
					</DialogDescription>
				</DialogHeader>
				<AnimatePresence mode="wait">
					{preview ? (
						<motion.div
							animate={{ opacity: 1, y: 0 }}
							className="min-h-0 space-y-4"
							initial={{ opacity: 0, y: 8 }}
							key={preview.path}
							transition={{ duration: 0.16 }}
						>
							<PatchApplyPreview
								disabled={isApplying}
								onReverseChange={onReverseChange}
								onStageChangesChange={onStageChangesChange}
								preview={preview}
								reverse={reverse}
								stageChanges={stageChanges}
							/>
						</motion.div>
					) : null}
				</AnimatePresence>
				<DialogFooter>
					<DialogClose
						disabled={isApplying}
						render={<Button variant="outline" />}
					>
						Cancel
					</DialogClose>
					<Button disabled={isApplying || !preview?.path} onClick={onApply}>
						{isApplying ? (
							<LoaderCircle className="animate-spin" />
						) : (
							<FileDiff />
						)}
						{isApplying ? "Checking and applying…" : "Apply patch"}
					</Button>
				</DialogFooter>
			</DialogContent>
		</Dialog>
	);
}
