export const ROW_HEIGHT = 22;
export const OVERSCAN = 36;
export const LANE_GAP = 14;
export const GRAPH_PADDING_LEFT = 18;
export const ROW_CENTER_Y = ROW_HEIGHT / 2;
export const GRAPH_STROKE_WIDTH = 2.2;
export const GRAPH_LINE_OVERLAP = 1;
export const GRAPH_TOP_Y = -GRAPH_LINE_OVERLAP;
export const GRAPH_BOTTOM_Y = ROW_HEIGHT + GRAPH_LINE_OVERLAP;
export const GRAPH_CURVE_MASK_WIDTH = GRAPH_STROKE_WIDTH + 2.4;

export const LANE_COLORS = [
	"#12b7ff",
	"#2d7dff",
	"#c329d6",
	"#ff2bb8",
	"#ef4444",
	"#fb6a2a",
	"#f0b429",
	"#6fd34f",
	"#24c8a0",
	"#15a1d8",
];

export type ColKey = "branch" | "graph" | "message" | "hash" | "author";

export const COL_ORDER: ColKey[] = [
	"branch",
	"graph",
	"message",
	"hash",
	"author",
];

export const COL_MIN: Record<ColKey, number> = {
	author: 170,
	branch: 100,
	graph: 220,
	hash: 88,
	message: 260,
};

export const COL_DEFAULT: Record<ColKey, number> = {
	author: 240,
	branch: 120,
	graph: 380,
	hash: 110,
	message: 520,
};
