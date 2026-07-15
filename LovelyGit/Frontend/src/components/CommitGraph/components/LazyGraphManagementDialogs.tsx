import { type ComponentProps, lazy, type ReactNode, Suspense } from "react";
import { SurfaceLoading } from "@/AppLazySurfaces";
import type { BranchComparisonDialog } from "./BranchComparisonDialog";
import type { BranchUpstreamDialog } from "./BranchUpstreamDialog";
import type { CheckoutTagDialog } from "./CheckoutTagDialog";
import type { CommitComparisonDialog } from "./CommitComparisonDialog";
import type { CreateTagDialog } from "./CreateTagDialog";
import type { CreateWorktreeDialog } from "./CreateWorktreeDialog";
import {
	DeferredBranchComparisonDialog,
	DeferredBranchUpstreamDialog,
	DeferredDeleteBranchDialog,
	DeferredRenameBranchDialog,
} from "./DeferredGraphManagementDialogs";
import type { DeleteBranchDialog } from "./DeleteBranchDialog";
import type { DeleteRemoteTagDialog } from "./DeleteRemoteTagDialog";
import type { DeleteTagDialog } from "./DeleteTagDialog";
import type { LockWorktreeDialog } from "./LockWorktreeDialog";
import type { RemoveWorktreeDialog } from "./RemoveWorktreeDialog";
import type { RenameBranchDialog } from "./RenameBranchDialog";

const CommitComparison = lazy(() =>
	importDialog(
		"CommitComparisonDialog",
		() => import("./CommitComparisonDialog"),
	),
);
const CheckoutTag = lazy(() =>
	importDialog("CheckoutTagDialog", () => import("./CheckoutTagDialog")),
);
const CreateTag = lazy(() =>
	importDialog("CreateTagDialog", () => import("./CreateTagDialog")),
);
const CreateWorktree = lazy(() =>
	importDialog("CreateWorktreeDialog", () => import("./CreateWorktreeDialog")),
);
const DeleteRemoteTag = lazy(() =>
	importDialog(
		"DeleteRemoteTagDialog",
		() => import("./DeleteRemoteTagDialog"),
	),
);
const DeleteTag = lazy(() =>
	importDialog("DeleteTagDialog", () => import("./DeleteTagDialog")),
);
const LockWorktree = lazy(() =>
	importDialog("LockWorktreeDialog", () => import("./LockWorktreeDialog")),
);
const RemoveWorktree = lazy(() =>
	importDialog("RemoveWorktreeDialog", () => import("./RemoveWorktreeDialog")),
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
	props: ComponentProps<typeof CommitComparisonDialog>,
) {
	return <Boundary>{<CommitComparison {...props} />}</Boundary>;
}
export function LazyCheckoutTagDialog(
	props: ComponentProps<typeof CheckoutTagDialog>,
) {
	return <Boundary>{<CheckoutTag {...props} />}</Boundary>;
}
export function LazyCreateTagDialog(
	props: ComponentProps<typeof CreateTagDialog>,
) {
	return <Boundary>{<CreateTag {...props} />}</Boundary>;
}
export function LazyCreateWorktreeDialog(
	props: ComponentProps<typeof CreateWorktreeDialog>,
) {
	return props.branchName !== null ? (
		<Boundary>{<CreateWorktree {...props} />}</Boundary>
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
	return props.tagName ? <Boundary>{<DeleteTag {...props} />}</Boundary> : null;
}
export function LazyLockWorktreeDialog(
	props: ComponentProps<typeof LockWorktreeDialog>,
) {
	return <Boundary>{<LockWorktree {...props} />}</Boundary>;
}
export function LazyRemoveWorktreeDialog(
	props: ComponentProps<typeof RemoveWorktreeDialog>,
) {
	return <Boundary>{<RemoveWorktree {...props} />}</Boundary>;
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
