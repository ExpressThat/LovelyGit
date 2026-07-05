import type { Settings } from "../Settings";

export type FontOption = {
	description: string;
	label: string;
	sample: string;
	stack: string;
	value: string;
};

type LocalFontData = {
	family: string;
	fullName?: string;
};

type LocalFontWindow = Window &
	typeof globalThis & {
		queryLocalFonts?: () => Promise<LocalFontData[]>;
	};

export const fontPreviewText = "Working tree ready";

const fallbackFontFamilies = [
	"Aptos",
	"Arial",
	"Bahnschrift",
	"Calibri",
	"Cambria",
	"Candara",
	"Century Gothic",
	"Comic Sans MS",
	"Consolas",
	"Constantia",
	"Corbel",
	"Courier New",
	"Ebrima",
	"Franklin Gothic Medium",
	"Gabriola",
	"Gadugi",
	"Georgia",
	"Gill Sans",
	"Impact",
	"Lucida Console",
	"Lucida Sans Unicode",
	"Malgun Gothic",
	"Microsoft JhengHei",
	"Microsoft New Tai Lue",
	"Microsoft PhagsPa",
	"Microsoft Sans Serif",
	"Microsoft Tai Le",
	"Microsoft YaHei",
	"Microsoft Yi Baiti",
	"MingLiU-ExtB",
	"Mongolian Baiti",
	"MS Gothic",
	"MV Boli",
	"Myanmar Text",
	"Nirmala UI",
	"Palatino Linotype",
	"Segoe Fluent Icons",
	"Segoe MDL2 Assets",
	"Segoe Print",
	"Segoe Script",
	"Segoe UI",
	"Segoe UI Emoji",
	"Segoe UI Historic",
	"Segoe UI Symbol",
	"SimSun",
	"Sylfaen",
	"Symbol",
	"Tahoma",
	"Times New Roman",
	"Trebuchet MS",
	"Verdana",
	"Webdings",
	"Wingdings",
	"Yu Gothic",
];

export const builtInFontOptions: FontOption[] = [
	{
		description: "LovelyGit's default compact interface face.",
		label: "Inter",
		sample: fontPreviewText,
		stack: '"Inter Variable", Inter, sans-serif',
		value: "Inter",
	},
	{
		description: "Uses the operating system UI font.",
		label: "System UI",
		sample: fontPreviewText,
		stack:
			'system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
		value: "System",
	},
];

export async function loadAvailableFontOptions(): Promise<FontOption[]> {
	const localFonts = await queryLocalFontFamilies();
	if (localFonts.length > 0) {
		return mergeFontOptions(
			builtInFontOptions,
			localFonts.map((family) => createLocalFontOption(family)),
		);
	}

	return mergeFontOptions(
		builtInFontOptions,
		fallbackFontFamilies
			.filter(isFontFamilyAvailable)
			.map((family) => createLocalFontOption(family)),
	);
}

export function applyFontsToDocument(uiFont: string, codeFont: string) {
	const uiOption = getFontOption(uiFont);
	const codeOption = getFontOption(codeFont);
	document.documentElement.style.setProperty(
		"--app-font-family",
		uiOption.stack,
	);
	document.documentElement.style.setProperty("--font-sans", uiOption.stack);
	document.documentElement.style.setProperty("--font-mono", codeOption.stack);
	document.documentElement.style.setProperty("--mono", codeOption.stack);
	document.documentElement.style.fontFamily = uiOption.stack;
	document.body.style.fontFamily = uiOption.stack;
	document
		.getElementById("root")
		?.style.setProperty("font-family", uiOption.stack);
	document.documentElement.dataset.font = uiOption.value;
	document.documentElement.dataset.codeFont = codeOption.value;
}

export function applyFontToDocument(font: Settings["Font"]) {
	applyFontsToDocument(font, font);
}

export function getFontOption(font: string) {
	return (
		builtInFontOptions.find((candidate) => candidate.value === font) ??
		createLocalFontOption(font)
	);
}

async function queryLocalFontFamilies() {
	const queryLocalFonts = (window as LocalFontWindow).queryLocalFonts;
	if (!queryLocalFonts) {
		return [];
	}

	try {
		const fonts = await queryLocalFonts();
		return [...new Set(fonts.map((font) => font.family).filter(Boolean))].sort(
			(left, right) => left.localeCompare(right),
		);
	} catch (error) {
		console.info("Local font access unavailable; using detected fonts.", error);
		return [];
	}
}

function createLocalFontOption(family: string): FontOption {
	return {
		description: "Installed on this device.",
		label: family,
		sample: fontPreviewText,
		stack: `${quoteFontFamily(family)}, ${fallbackForFamily(family)}`,
		value: family,
	};
}

function mergeFontOptions(...groups: FontOption[][]) {
	const options = new Map<string, FontOption>();
	for (const group of groups) {
		for (const option of group) {
			if (!options.has(option.value)) {
				options.set(option.value, option);
			}
		}
	}
	return [...options.values()];
}

function quoteFontFamily(family: string) {
	return `"${family.replaceAll('"', '\\"')}"`;
}

function fallbackForFamily(family: string) {
	const lower = family.toLowerCase();
	if (
		lower.includes("mono") ||
		lower.includes("console") ||
		lower.includes("code")
	) {
		return "monospace";
	}
	if (
		lower.includes("serif") ||
		lower.includes("georgia") ||
		lower.includes("cambria") ||
		lower.includes("times")
	) {
		return "serif";
	}
	return "sans-serif";
}

function isFontFamilyAvailable(family: string) {
	const canvas = document.createElement("canvas");
	const context = canvas.getContext("2d");
	if (!context) {
		return false;
	}

	const sample = "mmmmmmmmmwwwwwwwiiiiiiiii";
	const size = "72px";
	const fallback = fallbackForFamily(family);
	context.font = `${size} ${fallback}`;
	const fallbackWidth = context.measureText(sample).width;
	context.font = `${size} ${quoteFontFamily(family)}, ${fallback}`;
	const fontWidth = context.measureText(sample).width;
	return Math.abs(fontWidth - fallbackWidth) > 0.1;
}
