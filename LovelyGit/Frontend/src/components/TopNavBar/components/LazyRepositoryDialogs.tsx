import { type ComponentProps, lazy, Suspense } from "react";
import { SurfaceLoading } from "@/AppLazySurfaces";
import type { BranchIntegrationDialog } from "./BranchIntegrationDialog";
import type { CreateBranchDialog } from "./CreateBranchDialog";
import type { RemoteManagerDialog } from "./RemoteManagerDialog";

const CreateBranchDialogContent = lazy(() =>
	import("./CreateBranchDialog").then((module) => ({
		default: module.CreateBranchDialog,
	})),
);
const BranchIntegrationDialogContent = lazy(() =>
	import("./BranchIntegrationDialog").then((module) => ({
		default: module.BranchIntegrationDialog,
	})),
);
const RemoteManagerDialogContent = lazy(() =>
	import("./RemoteManagerDialog").then((module) => ({
		default: module.RemoteManagerDialog,
	})),
);

export function LazyCreateBranchDialog(
	props: ComponentProps<typeof CreateBranchDialog>,
) {
	if (!props.open) return null;
	return (
		<Suspense
			fallback={<SurfaceLoading label="Opening branch dialog" overlay />}
		>
			<CreateBranchDialogContent {...props} />
		</Suspense>
	);
}

export function LazyBranchIntegrationDialog(
	props: ComponentProps<typeof BranchIntegrationDialog>,
) {
	if (!props.mode) return null;
	return (
		<Suspense
			fallback={<SurfaceLoading label="Opening Git operation" overlay />}
		>
			<BranchIntegrationDialogContent {...props} />
		</Suspense>
	);
}

export function LazyRemoteManagerDialog(
	props: ComponentProps<typeof RemoteManagerDialog>,
) {
	if (!props.open) return null;
	return (
		<Suspense fallback={<SurfaceLoading label="Opening remotes" overlay />}>
			<RemoteManagerDialogContent {...props} />
		</Suspense>
	);
}
