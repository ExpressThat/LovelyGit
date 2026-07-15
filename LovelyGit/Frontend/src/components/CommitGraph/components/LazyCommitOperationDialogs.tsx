import type { ComponentProps } from "react";
import type { CheckoutCommitDialog } from "./CheckoutCommitDialog";
import type { CherryPickDialog } from "./CherryPickDialog";
import {
	DeferredCheckoutCommitDialog,
	DeferredCherryPickDialog,
	DeferredInteractiveRebaseDialog,
	DeferredReflogDialog,
	DeferredReflogResetDialog,
	DeferredResetCommitDialog,
	DeferredRevertDialog,
} from "./DeferredCommitOperationDialogs";
import type { InteractiveRebaseDialog } from "./InteractiveRebaseDialog";
import type { ReflogDialog } from "./ReflogDialog";
import type { ReflogResetDialog } from "./ReflogResetDialog";
import type { ResetCommitDialog } from "./ResetCommitDialog";
import type { RevertDialog } from "./RevertDialog";

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
	return <DeferredReflogDialog {...props} />;
}
export function LazyReflogResetDialog(
	props: ComponentProps<typeof ReflogResetDialog>,
) {
	return <DeferredReflogResetDialog {...props} />;
}
export function LazyResetCommitDialog(
	props: ComponentProps<typeof ResetCommitDialog>,
) {
	return props.commit ? <DeferredResetCommitDialog {...props} /> : null;
}
export function LazyRevertDialog(props: ComponentProps<typeof RevertDialog>) {
	return props.commits?.length ? <DeferredRevertDialog {...props} /> : null;
}
