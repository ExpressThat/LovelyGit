import type { Settings } from "../Settings";

export type FontOption = {
	description: string;
	label: string;
	sample: string;
	stack: string;
	value: Settings["Font"];
};

export const fontOptions: FontOption[] = [
	{
		description: "LovelyGit's default compact interface face.",
		label: "Inter",
		sample: "Branch graph 12:45",
		stack: '"Inter Variable", Inter, sans-serif',
		value: "Inter",
	},
	{
		description: "Uses the operating system UI font.",
		label: "System UI",
		sample: "Working tree ready",
		stack:
			'system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
		value: "System",
	},
	{
		description: "Windows-native, familiar, and readable.",
		label: "Segoe",
		sample: "Commit message",
		stack: '"Segoe UI", system-ui, sans-serif',
		value: "Segoe",
	},
	{
		description: "Modern Office-style spacing with a soft feel.",
		label: "Aptos",
		sample: "Review selected files",
		stack: 'Aptos, "Segoe UI", system-ui, sans-serif',
		value: "Aptos",
	},
	{
		description: "Wide forms for dense tables and long names.",
		label: "Verdana",
		sample: "origin/main updated",
		stack: 'Verdana, "Segoe UI", sans-serif',
		value: "Verdana",
	},
	{
		description: "Friendly shapes with a little more motion.",
		label: "Trebuchet",
		sample: "Push complete",
		stack: '"Trebuchet MS", "Segoe UI", sans-serif',
		value: "Trebuchet",
	},
	{
		description: "Serif rhythm for a calmer reading surface.",
		label: "Georgia",
		sample: "Merge branch notes",
		stack: 'Georgia, "Times New Roman", serif',
		value: "Georgia",
	},
	{
		description: "Document-like serif with crisp headings.",
		label: "Cambria",
		sample: "Patch summary",
		stack: "Cambria, Georgia, serif",
		value: "Cambria",
	},
	{
		description: "Classic monospace for code-heavy workflows.",
		label: "Consolas",
		sample: "src/App.tsx",
		stack: 'Consolas, "Cascadia Mono", monospace',
		value: "Consolas",
	},
	{
		description: "Uses the browser's default monospace stack.",
		label: "Mono",
		sample: "git status --short",
		stack:
			'"Cascadia Mono", "SFMono-Regular", Menlo, Monaco, Consolas, monospace',
		value: "Mono",
	},
];

export function applyFontToDocument(font: Settings["Font"]) {
	const option =
		fontOptions.find((candidate) => candidate.value === font) ?? fontOptions[0];
	document.documentElement.style.setProperty("--app-font-family", option.stack);
	document.documentElement.style.setProperty("--font-sans", option.stack);
	document.documentElement.style.setProperty("--font-mono", option.stack);
	document.documentElement.style.setProperty("--mono", option.stack);
	document.documentElement.style.fontFamily = option.stack;
	document.body.style.fontFamily = option.stack;
	document
		.getElementById("root")
		?.style.setProperty("font-family", option.stack);
	document.documentElement.dataset.font = option.value;
}
