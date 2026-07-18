import type { ComponentProps } from "react";
import type { FileBlameDialog as FileBlameComponent } from "@/components/FileBlame/FileBlameDialog";
import { createDeferredLoader } from "./deferredLoader";

export type FileBlameDialogProps = ComponentProps<typeof FileBlameComponent>;

export const fileBlameDialogLoader = createDeferredLoader(() =>
	import("@/components/FileBlame/FileBlameDialog").then(
		(module) => module.FileBlameDialog,
	),
);

export function preloadFileBlameDialog() {
	void fileBlameDialogLoader.load().catch(() => undefined);
}
