import { type ComponentProps, lazy, type ReactNode, Suspense } from "react";
import { SurfaceLoading } from "@/AppLazySurfaces";
import type { CheckoutCommitDialog } from "./CheckoutCommitDialog";
import type { CherryPickDialog } from "./CherryPickDialog";
import type { InteractiveRebaseDialog } from "./InteractiveRebaseDialog";
import type { ReflogDialog } from "./ReflogDialog";
import type { ReflogResetDialog } from "./ReflogResetDialog";
import type { ResetCommitDialog } from "./ResetCommitDialog";
import type { RevertDialog } from "./RevertDialog";

const CherryPick = lazy(() =>
	importDialog("CherryPickDialog", () => import("./CherryPickDialog")),
);
const CheckoutCommit = lazy(() =>
	importDialog("CheckoutCommitDialog", () => import("./CheckoutCommitDialog")),
);
const InteractiveRebase = lazy(() =>
	importDialog(
		"InteractiveRebaseDialog",
		() => import("./InteractiveRebaseDialog"),
	),
);
const Reflog = lazy(() =>
	importDialog("ReflogDialog", () => import("./ReflogDialog")),
);
const ReflogReset = lazy(() =>
	importDialog("ReflogResetDialog", () => import("./ReflogResetDialog")),
);
const ResetCommit = lazy(() =>
	importDialog("ResetCommitDialog", () => import("./ResetCommitDialog")),
);
const Revert = lazy(() =>
	importDialog("RevertDialog", () => import("./RevertDialog")),
);

export function LazyCherryPickDialog(
	props: ComponentProps<typeof CherryPickDialog>,
) {
	return props.commits?.length ? (
		<Boundary>{<CherryPick {...props} />}</Boundary>
	) : null;
}
export function LazyCheckoutCommitDialog(
	props: ComponentProps<typeof CheckoutCommitDialog>,
) {
	return <Boundary>{<CheckoutCommit {...props} />}</Boundary>;
}
export function LazyInteractiveRebaseDialog(
	props: ComponentProps<typeof InteractiveRebaseDialog>,
) {
	return props.baseCommit ? (
		<Boundary>{<InteractiveRebase {...props} />}</Boundary>
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
	return props.commit ? (
		<Boundary>{<ResetCommit {...props} />}</Boundary>
	) : null;
}
export function LazyRevertDialog(props: ComponentProps<typeof RevertDialog>) {
	return props.commits?.length ? (
		<Boundary>{<Revert {...props} />}</Boundary>
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
