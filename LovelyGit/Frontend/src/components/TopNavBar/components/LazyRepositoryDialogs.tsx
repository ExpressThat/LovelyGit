import type { ComponentProps, ComponentType, ReactNode } from "react";
import { SurfaceLoading } from "@/AppLazySurfaces";
import {
	type DeferredComponentLoader,
	DeferredPrimaryOverlay,
} from "@/AppPrimaryOverlays";
import { createDeferredLoader } from "@/lib/deferredLoader";
import type { BranchIntegrationDialog } from "./BranchIntegrationDialog";
import type { CreateBranchDialog } from "./CreateBranchDialog";
import type { RemoteManagerDialog } from "./RemoteManagerDialog";

type CreateBranchProps = ComponentProps<typeof CreateBranchDialog>;
type BranchIntegrationProps = ComponentProps<typeof BranchIntegrationDialog>;
type RemoteManagerProps = ComponentProps<typeof RemoteManagerDialog>;

const createBranchLoader = createDeferredLoader(() =>
	import("./CreateBranchDialog").then(
		(module) => module.CreateBranchDialog as ComponentType<CreateBranchProps>,
	),
);
const branchIntegrationLoader = createDeferredLoader(() =>
	import("./BranchIntegrationDialog").then(
		(module) =>
			module.BranchIntegrationDialog as ComponentType<BranchIntegrationProps>,
	),
);
const remoteManagerLoader = createDeferredLoader(() =>
	import("./RemoteManagerDialog").then(
		(module) => module.RemoteManagerDialog as ComponentType<RemoteManagerProps>,
	),
);

export function LazyCreateBranchDialog(props: CreateBranchProps) {
	if (!props.open) return null;
	return (
		<DeferredRepositoryDialog
			fallback={<SurfaceLoading label="Opening branch dialog" overlay />}
			loader={createBranchLoader}
			props={props}
		/>
	);
}

export function LazyBranchIntegrationDialog(props: BranchIntegrationProps) {
	if (!props.mode) return null;
	return (
		<DeferredRepositoryDialog
			fallback={<SurfaceLoading label="Opening Git operation" overlay />}
			loader={branchIntegrationLoader}
			props={props}
		/>
	);
}

export function LazyRemoteManagerDialog(props: RemoteManagerProps) {
	if (!props.open) return null;
	return (
		<DeferredRepositoryDialog
			fallback={<SurfaceLoading label="Opening remotes" overlay />}
			loader={remoteManagerLoader}
			props={props}
		/>
	);
}

function DeferredRepositoryDialog<TProps extends object>({
	fallback,
	loader,
	props,
}: {
	fallback: ReactNode;
	loader: DeferredComponentLoader<TProps>;
	props: TProps;
}) {
	return (
		<DeferredPrimaryOverlay fallback={fallback} loader={loader} props={props} />
	);
}
