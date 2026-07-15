import { type ComponentProps, lazy, type ReactNode, Suspense } from "react";
import { SurfaceLoading } from "@/AppLazySurfaces";
import type { CheckoutCommitDialog } from "./CheckoutCommitDialog";
import type { CherryPickDialog } from "./CherryPickDialog";
import {
	DeferredCheckoutCommitDialog,
	DeferredCherryPickDialog,
	DeferredInteractiveRebaseDialog,
	DeferredResetCommitDialog,
	DeferredRevertDialog,
} from "./DeferredCommitOperationDialogs";
import type { InteractiveRebaseDialog } from "./InteractiveRebaseDialog";
import type { ReflogDialog } from "./ReflogDialog";
import type { ReflogResetDialog } from "./ReflogResetDialog";
import type { ResetCommitDialog } from "./ResetCommitDialog";
import type { RevertDialog } from "./RevertDialog";

const Reflog = lazy(() =>
	importDialog("ReflogDialog", () => import("./ReflogDialog")),
);
const ReflogReset = lazy(() =>
	importDialog("ReflogResetDialog", () => import("./ReflogResetDialog")),
);
export function LazyCherryPickDialog(
	props: ComponentProps<typeof CherryPickDialog>,
) {
	return props.commits?.length ? <DeferredCherryPickDialog {...props} /> : null;
}
export function LazyCheckoutCommitDialog(
	props: ComponentProps<typeof CheckoutCommitDialog>,
) {
	return <DeferredCheckoutCommitDialog {...props} />;
}
export function LazyInteractiveRebaseDialog(
	props: ComponentProps<typeof InteractiveRebaseDialog>,
) {
	return props.baseCommit ? (
		<DeferredInteractiveRebaseDialog {...props} />
	) : null;
}
export function LazyReflogDialog(props: ComponentProps<typeof ReflogDialog>) {
	return <Boundary>{<Reflog {...props} />}</Boundary>;
}
export function LazyReflogResetDialog(
	props: ComponentProps<typeof ReflogResetDialog>,
) {
	return <Boundary>{<ReflogReset {...props} />}</Boundary>;
}
export function LazyResetCommitDialog(
	props: ComponentProps<typeof ResetCommitDialog>,
) {
	return props.commit ? <DeferredResetCommitDialog {...props} /> : null;
}
export function LazyRevertDialog(props: ComponentProps<typeof RevertDialog>) {
	return props.commits?.length ? <DeferredRevertDialog {...props} /> : null;
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
