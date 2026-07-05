import { useEffect } from "react";
import { useSetting } from "@/lib/settings/settingsStore";
import { applyFontToDocument } from "./fontUtils";

export function useApplyFont() {
	const font = useSetting("Font");

	useEffect(() => {
		applyFontToDocument(font);
	}, [font]);
}
