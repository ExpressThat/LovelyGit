import { type ComponentProps, lazy, type ReactNode, Suspense } from "react";
import { SurfaceLoading } from "@/AppLazySurfaces";
import type { BranchComparisonDialog } from "./BranchComparisonDialog";
import type { BranchUpstreamDialog } from "./BranchUpstreamDialog";
import type { CheckoutTagDialog } from "./CheckoutTagDialog";
import type { CreateTagDialog } from "./CreateTagDialog";
import type { CreateWorktreeDialog } from "./CreateWorktreeDialog";
import {
	DeferredBranchComparisonDialog,
	DeferredBranchUpstreamDialog,
	DeferredCommitComparisonDialog,
	DeferredDeleteBranchDialog,
	DeferredRenameBranchDialog,
} from "./DeferredGraphManagementDialogs";
import {
	DeferredCheckoutTagDialog,
	DeferredCreateTagDialog,
	DeferredDeleteTagDialog,
} from "./DeferredTagManagementDialogs";
import {
	DeferredCreateWorktreeDialog,
	DeferredLockWorktreeDialog,
	DeferredRemoveWorktreeDialog,
} from "./DeferredWorktreeDialogs";
import type { DeleteBranchDialog } from "./DeleteBranchDialog";
import type { DeleteRemoteTagDialog } from "./DeleteRemoteTagDialog";
import type { DeleteTagDialog } from "./DeleteTagDialog";
import type { LockWorktreeDialog } from "./LockWorktreeDialog";
import type { RemoveWorktreeDialog } from "./RemoveWorktreeDialog";
import type { RenameBranchDialog } from "./RenameBranchDialog";

const DeleteRemoteTag = lazy(() =>
	importDialog(
		"DeleteRemoteTagDialog",
		() => import("./DeleteRemoteTagDialog"),
	),
);

export function LazyBranchComparisonDialog(
	props: ComponentProps<typeof BranchComparisonDialog>,
) {
	return props.targetBranchName ? (
		<DeferredBranchComparisonDialog {...props} />
	) : null;
}
export function LazyBranchUpstreamDialog(
	props: ComponentProps<typeof BranchUpstreamDialog>,
) {
	return props.branchName ? <DeferredBranchUpstreamDialog {...props} /> : null;
}
export function LazyCommitComparisonDialog(
	props: ComponentProps<typeof DeferredCommitComparisonDialog>,
) {
	return <DeferredCommitComparisonDialog {...props} />;
}
export function LazyCheckoutTagDialog(
	props: ComponentProps<typeof CheckoutTagDialog>,
) {
	return <DeferredCheckoutTagDialog {...props} />;
}
export function LazyCreateTagDialog(
	props: ComponentProps<typeof CreateTagDialog>,
) {
	return <DeferredCreateTagDialog {...props} />;
}
export function LazyCreateWorktreeDialog(
	props: ComponentProps<typeof CreateWorktreeDialog>,
) {
	return props.branchName !== null ? (
		<DeferredCreateWorktreeDialog {...props} />
	) : null;
}
export function LazyDeleteBranchDialog(
	props: ComponentProps<typeof DeleteBranchDialog>,
) {
	return props.branchName ? <DeferredDeleteBranchDialog {...props} /> : null;
}
export function LazyDeleteRemoteTagDialog(
	props: ComponentProps<typeof DeleteRemoteTagDialog>,
) {
	return props.tagName ? (
		<Boundary>{<DeleteRemoteTag {...props} />}</Boundary>
	) : null;
}
export function LazyDeleteTagDialog(
	props: ComponentProps<typeof DeleteTagDialog>,
) {
	return props.tagName ? <DeferredDeleteTagDialog {...props} /> : null;
}
export function LazyLockWorktreeDialog(
	props: ComponentProps<typeof LockWorktreeDialog>,
) {
	return <DeferredLockWorktreeDialog {...props} />;
}
export function LazyRemoveWorktreeDialog(
	props: ComponentProps<typeof RemoveWorktreeDialog>,
) {
	return <DeferredRemoveWorktreeDialog {...props} />;
}
export function LazyRenameBranchDialog(
	props: ComponentProps<typeof RenameBranchDialog>,
) {
	return props.branchName ? <DeferredRenameBranchDialog {...props} /> : null;
}

function Boundary({ children }: { children: ReactNode }) {
	return (
		<Suspense
			fallback={<SurfaceLoading label="Opening Git operation" overlay />}
		>
			{children}
		</Suspense>
	);
}

async function importDialog<
	TName extends string,
	TModule extends Record<TName, unknown>,
>(name: TName, load: () => Promise<TModule>) {
	const module = await load();
	return { default: module[name] as TModule[TName] };
}
