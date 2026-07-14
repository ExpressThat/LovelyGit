import { useEffect, useState } from "react";

export const overlayExitRetentionMs = 300;

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
			overlayExitRetentionMs,
		);
		return () => window.clearTimeout(release);
	}, [active, retained]);
	return retained;
}
