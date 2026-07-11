import { useState } from "react";

export function useRefsAccordionLayout(ids: string[]) {
	const [closed, setClosed] = useState<Set<string>>(() => new Set());
	const [weights, setWeights] = useState<Record<string, number>>(() =>
		Object.fromEntries(ids.map((id) => [id, 1])),
	);
	const openIds = ids.filter((id) => !closed.has(id));
	const toggle = (id: string) => {
		setClosed((current) => {
			const next = new Set(current);
			if (next.has(id)) next.delete(id);
			else next.add(id);
			return next;
		});
	};
	const resize = (before: string, after: string, delta: number) => {
		setWeights((current) => {
			const total = (current[before] ?? 1) + (current[after] ?? 1);
			const nextBefore = Math.min(
				total - 0.2,
				Math.max(0.2, (current[before] ?? 1) + delta),
			);
			return { ...current, [after]: total - nextBefore, [before]: nextBefore };
		});
	};
	return { closed, openIds, resize, toggle, weights };
}
