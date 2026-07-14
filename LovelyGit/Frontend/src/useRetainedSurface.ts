import { useEffect, useState } from "react";

export const overlayIdleRetentionMs = 30_000;

export function useRetainedSurface(active: boolean) {
	const [retained, setRetained] = useState(active);
	useEffect(() => {
		if (active) {
			setRetained(true);
			return;
		}
		if (!retained) return;
		const release = window.setTimeout(
			() => setRetained(false),
			overlayIdleRetentionMs,
		);
		return () => window.clearTimeout(release);
	}, [active, retained]);
	return retained;
}
