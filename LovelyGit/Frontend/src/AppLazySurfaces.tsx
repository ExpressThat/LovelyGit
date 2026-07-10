import { LoaderCircle } from "lucide-react";
import { motion, useReducedMotion } from "motion/react";
import { type ComponentProps, lazy, Suspense } from "react";
import type { CommitDetails as CommitDetailsComponent } from "./components/CommitDetails/CommitDetails";
import type { CommitFileDiffView as CommitFileDiffComponent } from "./components/CommitFileDiff/CommitFileDiffView";
import type { WorkingChangesPanel as WorkingChangesComponent } from "./components/WorkingChanges/WorkingChangesPanel";
import type { WorkingTreeFileDiffView as WorkingTreeDiffComponent } from "./components/WorkingChanges/WorkingTreeFileDiffView";

const LazyCommitDetails = lazy(() =>
	import("./components/CommitDetails/CommitDetails").then((module) => ({
		default: module.CommitDetails,
	})),
);
const LazyCommitFileDiff = lazy(() =>
	import("./components/CommitFileDiff/CommitFileDiffView").then((module) => ({
		default: module.CommitFileDiffView,
	})),
);
const LazyWorkingChanges = lazy(() =>
	import("./components/WorkingChanges/WorkingChangesPanel").then((module) => ({
		default: module.WorkingChangesPanel,
	})),
);
const LazyWorkingTreeDiff = lazy(() =>
	import("./components/WorkingChanges/WorkingTreeFileDiffView").then(
		(module) => ({ default: module.WorkingTreeFileDiffView }),
	),
);

export function CommitDetailsSurface(
	props: ComponentProps<typeof CommitDetailsComponent>,
) {
	return (
		<Suspense fallback={<SurfaceLoading label="Loading commit details" />}>
			<LazyCommitDetails {...props} />
		</Suspense>
	);
}

export function CommitFileDiffSurface(
	props: ComponentProps<typeof CommitFileDiffComponent>,
) {
	return (
		<Suspense fallback={<SurfaceLoading label="Preparing commit diff" fill />}>
			<LazyCommitFileDiff {...props} />
		</Suspense>
	);
}

export function WorkingChangesSurface(
	props: ComponentProps<typeof WorkingChangesComponent>,
) {
	return (
		<Suspense fallback={<SurfaceLoading label="Loading working changes" />}>
			<LazyWorkingChanges {...props} />
		</Suspense>
	);
}

export function WorkingTreeDiffSurface(
	props: ComponentProps<typeof WorkingTreeDiffComponent>,
) {
	return (
		<Suspense fallback={<SurfaceLoading label="Preparing working diff" fill />}>
			<LazyWorkingTreeDiff {...props} />
		</Suspense>
	);
}

export function SurfaceLoading({
	fill = false,
	label,
	overlay = false,
}: {
	fill?: boolean;
	label: string;
	overlay?: boolean;
}) {
	const reduceMotion = useReducedMotion();
	return (
		<motion.div
			animate={{ opacity: 1, y: 0 }}
			aria-label={label}
			className={`grid place-items-center text-muted-foreground ${
				overlay
					? "fixed inset-0 z-50 bg-background/40 backdrop-blur-sm"
					: fill
						? "h-full bg-background"
						: "min-h-32 bg-background"
			}`}
			initial={{ opacity: 0, y: reduceMotion ? 0 : 4 }}
			role="status"
			transition={{ duration: reduceMotion ? 0 : 0.16 }}
		>
			<div className="flex items-center gap-2 text-xs">
				<LoaderCircle aria-hidden="true" className="size-4 animate-spin" />
				{label}…
			</div>
		</motion.div>
	);
}
