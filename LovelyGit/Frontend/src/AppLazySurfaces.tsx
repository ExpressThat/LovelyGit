import { motion, useReducedMotion } from "motion/react";
import {
	type ComponentProps,
	type ComponentType,
	lazy,
	Suspense,
	useEffect,
	useState,
} from "react";
import { LoaderCircle } from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import { createDeferredLoader } from "@/lib/deferredLoader";
import { CommitDetails } from "./components/CommitDetails/CommitDetails";
import type { CommitFileDiffView as CommitFileDiffComponent } from "./components/CommitFileDiff/CommitFileDiffView";
import type { ConflictResolutionView as ConflictResolutionComponent } from "./components/ConflictResolution/ConflictResolutionView";
import type { WorkingChangesPanel as WorkingChangesComponent } from "./components/WorkingChanges/WorkingChangesPanel";
import type { WorkingTreeFileDiffView as WorkingTreeDiffComponent } from "./components/WorkingChanges/WorkingTreeFileDiffView";

const commitFileDiffLoader = createDeferredLoader(() =>
	import("./components/CommitFileDiff/CommitFileDiffView").then(
		(module) => module.CommitFileDiffView,
	),
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
type ConflictResolutionProps = ComponentProps<
	typeof ConflictResolutionComponent
>;
const conflictResolutionLoader = createDeferredLoader(() =>
	import("./components/ConflictResolution/ConflictResolutionView").then(
		(module) => module.ConflictResolutionView,
	),
);

export function CommitDetailsSurface(
	props: ComponentProps<typeof CommitDetails>,
) {
	return <CommitDetails {...props} />;
}

export function CommitFileDiffSurface(
	props: ComponentProps<typeof CommitFileDiffComponent>,
) {
	const [Component, setComponent] = useState<ComponentType<
		ComponentProps<typeof CommitFileDiffComponent>
	> | null>(() => commitFileDiffLoader.get());
	const [error, setError] = useState<string | null>(null);
	const [attempt, setAttempt] = useState(0);
	useEffect(() => {
		void attempt;
		let active = true;
		commitFileDiffLoader.load().then(
			(loaded) => {
				if (active) setComponent(() => loaded);
			},
			(loadError: unknown) => {
				if (active) {
					setError(
						loadError instanceof Error
							? loadError.message
							: "Failed to load commit diff.",
					);
				}
			},
		);
		return () => {
			active = false;
		};
	}, [attempt]);
	if (Component) return <Component {...props} />;
	if (error) {
		return (
			<SurfaceLoadError
				message={error}
				onRetry={() => {
					setError(null);
					setAttempt((value) => value + 1);
				}}
			/>
		);
	}
	return <SurfaceLoading label="Preparing commit diff" fill />;
}

export function preloadCommitFileDiffSurface() {
	return commitFileDiffLoader.load().then(
		() => undefined,
		() => undefined,
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
	if (props.file.group === "Unmerged") {
		return (
			<DeferredConflictResolution {...(props as ConflictResolutionProps)} />
		);
	}
	return (
		<Suspense fallback={<SurfaceLoading label="Preparing working diff" fill />}>
			<LazyWorkingTreeDiff {...props} />
		</Suspense>
	);
}

function DeferredConflictResolution(props: ConflictResolutionProps) {
	const [Component, setComponent] =
		useState<ComponentType<ConflictResolutionProps> | null>(() =>
			conflictResolutionLoader.get(),
		);
	const [error, setError] = useState<string | null>(null);
	const [attempt, setAttempt] = useState(0);
	useEffect(() => {
		void attempt;
		let active = true;
		conflictResolutionLoader.load().then(
			(loaded) => {
				if (active) setComponent(() => loaded);
			},
			(loadError: unknown) => {
				if (active) {
					setError(
						loadError instanceof Error
							? loadError.message
							: "Failed to load conflict resolver.",
					);
				}
			},
		);
		return () => {
			active = false;
		};
	}, [attempt]);
	if (Component) return <Component {...props} />;
	if (error) {
		return (
			<SurfaceLoadError
				message={error}
				onRetry={() => {
					setError(null);
					setAttempt((value) => value + 1);
				}}
			/>
		);
	}
	return <SurfaceLoading label="Preparing conflict resolver" fill />;
}

function SurfaceLoadError({
	message,
	onRetry,
}: {
	message: string;
	onRetry: () => void;
}) {
	return (
		<div className="grid h-full place-items-center bg-background p-4 text-sm text-destructive">
			<div className="space-y-3 text-center">
				<p>{message}</p>
				<Button onClick={onRetry} type="button" variant="outline">
					Retry
				</Button>
			</div>
		</div>
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
