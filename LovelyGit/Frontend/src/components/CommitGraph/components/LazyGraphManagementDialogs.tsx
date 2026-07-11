import { type ComponentProps, lazy, type ReactNode, Suspense } from "react";
import { SurfaceLoading } from "@/AppLazySurfaces";
import type { BranchComparisonDialog } from "./BranchComparisonDialog";
import type { BranchUpstreamDialog } from "./BranchUpstreamDialog";
import type { CreateTagDialog } from "./CreateTagDialog";
import type { CreateWorktreeDialog } from "./CreateWorktreeDialog";
import type { DeleteBranchDialog } from "./DeleteBranchDialog";
import type { DeleteTagDialog } from "./DeleteTagDialog";
import type { LockWorktreeDialog } from "./LockWorktreeDialog";
import type { RemoveWorktreeDialog } from "./RemoveWorktreeDialog";
import type { RenameBranchDialog } from "./RenameBranchDialog";

const BranchComparison = lazy(() =>
	importDialog(
		"BranchComparisonDialog",
		() => import("./BranchComparisonDialog"),
	),
);
const BranchUpstream = lazy(() =>
	importDialog("BranchUpstreamDialog", () => import("./BranchUpstreamDialog")),
);
const CreateTag = lazy(() =>
	importDialog("CreateTagDialog", () => import("./CreateTagDialog")),
);
const CreateWorktree = lazy(() =>
	importDialog("CreateWorktreeDialog", () => import("./CreateWorktreeDialog")),
);
const DeleteBranch = lazy(() =>
	importDialog("DeleteBranchDialog", () => import("./DeleteBranchDialog")),
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
const RenameBranch = lazy(() =>
	importDialog("RenameBranchDialog", () => import("./RenameBranchDialog")),
);

export function LazyBranchComparisonDialog(
	props: ComponentProps<typeof BranchComparisonDialog>,
) {
	return props.targetBranchName ? (
		<Boundary>{<BranchComparison {...props} />}</Boundary>
	) : null;
}
export function LazyBranchUpstreamDialog(
	props: ComponentProps<typeof BranchUpstreamDialog>,
) {
	return props.branchName ? (
		<Boundary>{<BranchUpstream {...props} />}</Boundary>
	) : null;
}
export function LazyCreateTagDialog(
	props: ComponentProps<typeof CreateTagDialog>,
) {
	return <Boundary>{<CreateTag {...props} />}</Boundary>;
}
export function LazyCreateWorktreeDialog(
	props: ComponentProps<typeof CreateWorktreeDialog>,
) {
	return props.branchName ? (
		<Boundary>{<CreateWorktree {...props} />}</Boundary>
	) : null;
}
export function LazyDeleteBranchDialog(
	props: ComponentProps<typeof DeleteBranchDialog>,
) {
	return props.branchName ? (
		<Boundary>{<DeleteBranch {...props} />}</Boundary>
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
	return props.branchName ? (
		<Boundary>{<RenameBranch {...props} />}</Boundary>
	) : null;
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
