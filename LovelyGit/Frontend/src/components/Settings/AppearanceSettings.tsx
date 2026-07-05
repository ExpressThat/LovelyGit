import type { ReactNode } from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from "@/components/ui/select";
import {
	builtInFontOptions,
	type FontOption,
	getFontOption,
	loadAvailableFontOptions,
} from "@/lib/settings/font/fontUtils";
import {
	setSetting,
	setSettings,
	useSetting,
} from "@/lib/settings/settingsStore";
import {
	getThemeOption,
	type ThemeOption,
	themeOptions,
} from "@/lib/settings/theme/themeCatalog";

const lightThemeOptions = themeOptions.filter((option) => !option.isDark);
const darkThemeOptions = themeOptions.filter((option) => option.isDark);

export function AppearanceSettings() {
	const mode = useSetting("Theme");
	const lightTheme = useSetting("LightTheme");
	const darkTheme = useSetting("DarkTheme");
	const legacyFont = useSetting("Font");
	const legacyUiFont = useSetting("UiFont") || legacyFont;
	const legacyCodeFont = useSetting("CodeFont") || legacyFont;
	const lightUiFont = useSetting("LightUiFont") || legacyUiFont;
	const lightCodeFont = useSetting("LightCodeFont") || legacyCodeFont;
	const darkUiFont = useSetting("DarkUiFont") || legacyUiFont;
	const darkCodeFont = useSetting("DarkCodeFont") || legacyCodeFont;
	const lightAccent = useSetting("LightAccent");
	const lightBackground = useSetting("LightBackground");
	const lightForeground = useSetting("LightForeground");
	const darkAccent = useSetting("DarkAccent");
	const darkBackground = useSetting("DarkBackground");
	const darkForeground = useSetting("DarkForeground");
	const [availableFonts, setAvailableFonts] =
		useState<FontOption[]>(builtInFontOptions);
	const [dropdownBoundary, setDropdownBoundary] = useState<Element | null>(
		null,
	);
	const setAppearanceRoot = useCallback((element: HTMLDivElement | null) => {
		setDropdownBoundary(element?.closest("section.custom-scrollbar") ?? null);
	}, []);

	useEffect(() => {
		let isActive = true;
		loadAvailableFontOptions().then((options) => {
			if (!isActive) {
				return;
			}

			setAvailableFonts(options);
		});

		return () => {
			isActive = false;
		};
	}, []);

	const legacyThemeOption =
		mode === "System" || mode === "Light" || mode === "Dark"
			? null
			: getThemeOption(mode);
	const displayMode = legacyThemeOption
		? legacyThemeOption.isDark
			? "Dark"
			: "Light"
		: mode;
	const selectedLightTheme =
		legacyThemeOption && !legacyThemeOption.isDark
			? legacyThemeOption
			: getThemeOption(lightTheme, "Morning");
	const selectedDarkTheme = legacyThemeOption?.isDark
		? legacyThemeOption
		: getThemeOption(darkTheme, "Midnight");
	const previewCodeFont =
		displayMode === "Dark" ? darkCodeFont : lightCodeFont || darkCodeFont;
	const codeFontOption = useMemo(
		() => getFontOption(previewCodeFont),
		[previewCodeFont],
	);

	return (
		<div className="grid gap-6" ref={setAppearanceRoot}>
			<div>
				<h3 className="font-semibold text-lg">Appearance</h3>
				<p className="text-muted-foreground text-sm">
					Choose the app mode, paired palettes, and typefaces.
				</p>
			</div>

			<div className="grid grid-cols-3 gap-3">
				<ModePreview
					active={displayMode === "System"}
					label="System"
					onSelect={() => void setSetting("Theme", "System")}
					type="system"
				/>
				<ModePreview
					active={displayMode === "Light"}
					label="Light"
					onSelect={() => void setSetting("Theme", "Light")}
					type="light"
				/>
				<ModePreview
					active={displayMode === "Dark"}
					label="Dark"
					onSelect={() => void setSetting("Theme", "Dark")}
					type="dark"
				/>
			</div>

			<CodePreview
				codeFont={codeFontOption.stack}
				darkTheme={selectedDarkTheme}
				lightTheme={selectedLightTheme}
			/>

			<div className="grid gap-4">
				<ThemePanel
					accent={lightAccent}
					background={lightBackground}
					codeFont={lightCodeFont}
					fontOptions={availableFonts}
					foreground={lightForeground}
					onAccentChange={(value) => void setSetting("LightAccent", value)}
					onBackgroundChange={(value) =>
						void setSetting("LightBackground", value)
					}
					onCodeFontChange={(value) => void setSetting("LightCodeFont", value)}
					onForegroundChange={(value) =>
						void setSetting("LightForeground", value)
					}
					onThemeChange={(value) =>
						void setSettings({
							LightAccent: "",
							LightBackground: "",
							LightForeground: "",
							LightTheme: value,
						})
					}
					onUiFontChange={(value) => void setSetting("LightUiFont", value)}
					selectedTheme={selectedLightTheme}
					themeOptions={lightThemeOptions}
					title="Light theme"
					uiFont={lightUiFont}
					dropdownBoundary={dropdownBoundary}
				/>
				<ThemePanel
					accent={darkAccent}
					background={darkBackground}
					codeFont={darkCodeFont}
					fontOptions={availableFonts}
					foreground={darkForeground}
					onAccentChange={(value) => void setSetting("DarkAccent", value)}
					onBackgroundChange={(value) =>
						void setSetting("DarkBackground", value)
					}
					onCodeFontChange={(value) => void setSetting("DarkCodeFont", value)}
					onForegroundChange={(value) =>
						void setSetting("DarkForeground", value)
					}
					onThemeChange={(value) =>
						void setSettings({
							DarkAccent: "",
							DarkBackground: "",
							DarkForeground: "",
							DarkTheme: value,
						})
					}
					onUiFontChange={(value) => void setSetting("DarkUiFont", value)}
					selectedTheme={selectedDarkTheme}
					themeOptions={darkThemeOptions}
					title="Dark theme"
					uiFont={darkUiFont}
					dropdownBoundary={dropdownBoundary}
				/>
			</div>
		</div>
	);
}

function ModePreview({
	active,
	label,
	onSelect,
	type,
}: {
	active: boolean;
	label: string;
	onSelect: () => void;
	type: "dark" | "light" | "system";
}) {
	return (
		<button className="grid gap-2 text-center" onClick={onSelect} type="button">
			<div
				className={`relative h-24 overflow-hidden rounded-xl border bg-card transition ${active ? "border-primary ring-2 ring-ring/35" : "border-border"}`}
			>
				{type === "system" ? (
					<div className="absolute inset-0 grid grid-cols-2">
						<div className="bg-[#f3f4f6]" />
						<div className="bg-[#171923]" />
					</div>
				) : null}
				<div
					className={`absolute inset-0 ${type === "light" ? "bg-[#f7f7f8]" : ""} ${type === "dark" ? "bg-[#4a4a48]" : ""}`}
				/>
				<div className="absolute inset-x-4 bottom-0 h-14 rounded-t-lg bg-background/90 p-3 shadow-sm">
					<div className="mx-auto mb-3 h-2 w-20 rounded-full bg-muted-foreground/25" />
					<div className="grid gap-2">
						<div className="h-2 rounded-full bg-muted-foreground/20" />
						<div className="h-2 w-2/3 rounded-full bg-muted-foreground/20" />
					</div>
				</div>
			</div>
			<span className={active ? "text-foreground" : "text-muted-foreground"}>
				{label}
			</span>
		</button>
	);
}

function CodePreview({
	codeFont,
	darkTheme,
	lightTheme,
}: {
	codeFont: string;
	darkTheme: ThemeOption;
	lightTheme: ThemeOption;
}) {
	return (
		<div
			className="grid overflow-hidden rounded-lg border text-xs"
			style={{ fontFamily: codeFont }}
		>
			<div className="grid grid-cols-2">
				<CodePane option={lightTheme} side="left" />
				<CodePane option={darkTheme} side="right" />
			</div>
			<div className="flex h-5 items-center justify-between bg-muted px-2">
				<span className="size-3 rounded-full bg-muted-foreground/20" />
				<span className="size-3 rounded-full bg-muted-foreground/20" />
			</div>
		</div>
	);
}

function CodePane({
	option,
	side,
}: {
	option: ThemeOption;
	side: "left" | "right";
}) {
	return (
		<div
			className="grid grid-cols-[3rem_1fr] overflow-hidden"
			style={{
				background: option.variables.background,
				color: option.variables.foreground,
			}}
		>
			<div className="grid gap-1 border-r border-current/10 py-2 text-right text-muted-foreground">
				{[1, 2, 3, 4, 5].map((line) => (
					<span className="px-3" key={line}>
						{line}
					</span>
				))}
			</div>
			<div className="grid gap-1 py-2">
				<CodeLine text="const themePreview: ThemeConfig = {" />
				<CodeLine
					highlight={side === "left" ? "remove" : "add"}
					text={`surface: "${side === "left" ? "sidebar" : "sidebar-elevated"}",`}
				/>
				<CodeLine
					highlight={side === "left" ? "remove" : "add"}
					text={`accent: "${option.accent}",`}
				/>
				<CodeLine
					highlight={side === "left" ? "remove" : "add"}
					text={`contrast: ${option.isDark ? 68 : 42},`}
				/>
				<CodeLine text="};" />
			</div>
		</div>
	);
}

function CodeLine({
	highlight,
	text,
}: {
	highlight?: "add" | "remove";
	text: string;
}) {
	return (
		<div
			className={`px-3 ${highlight === "add" ? "bg-green-500/20 text-green-500" : ""} ${highlight === "remove" ? "bg-red-500/20 text-red-500" : ""}`}
		>
			{text}
		</div>
	);
}

function ThemePanel({
	accent,
	background,
	codeFont,
	dropdownBoundary,
	fontOptions,
	foreground,
	onAccentChange,
	onBackgroundChange,
	onCodeFontChange,
	onForegroundChange,
	onThemeChange,
	onUiFontChange,
	selectedTheme,
	themeOptions,
	title,
	uiFont,
}: {
	accent: string;
	background: string;
	codeFont: string;
	dropdownBoundary: Element | null;
	fontOptions: FontOption[];
	foreground: string;
	onAccentChange: (value: string) => void;
	onBackgroundChange: (value: string) => void;
	onCodeFontChange: (value: string) => void;
	onForegroundChange: (value: string) => void;
	onThemeChange: (value: string) => void;
	onUiFontChange: (value: string) => void;
	selectedTheme: ThemeOption;
	themeOptions: ThemeOption[];
	title: string;
	uiFont: string;
}) {
	return (
		<div className="overflow-hidden rounded-lg border bg-card">
			<div className="grid grid-cols-[1fr_auto] items-center gap-3 border-b px-4 py-2">
				<div className="font-medium text-sm">{title}</div>
				<ThemeSelect
					dropdownBoundary={dropdownBoundary}
					onValueChange={onThemeChange}
					options={themeOptions}
					value={selectedTheme.value}
				/>
			</div>
			<ThemeRow label="Accent">
				<ColorControl
					onChange={onAccentChange}
					presetColor={selectedTheme.accent}
					value={accent}
				/>
			</ThemeRow>
			<ThemeRow label="Background">
				<ColorControl
					onChange={onBackgroundChange}
					presetColor={selectedTheme.background}
					value={background}
				/>
			</ThemeRow>
			<ThemeRow label="Foreground">
				<ColorControl
					onChange={onForegroundChange}
					presetColor={selectedTheme.foreground}
					value={foreground}
				/>
			</ThemeRow>
			<ThemeRow label="UI font">
				<FontSelect
					dropdownBoundary={dropdownBoundary}
					onValueChange={onUiFontChange}
					options={fontOptions}
					value={uiFont}
				/>
			</ThemeRow>
			<ThemeRow label="Code font">
				<FontSelect
					dropdownBoundary={dropdownBoundary}
					onValueChange={onCodeFontChange}
					options={fontOptions}
					value={codeFont}
				/>
			</ThemeRow>
		</div>
	);
}

function ThemeRow({ children, label }: { children: ReactNode; label: string }) {
	return (
		<div className="grid grid-cols-[1fr_minmax(12rem,18rem)] items-center border-b px-4 py-2 last:border-b-0">
			<div className="text-sm">{label}</div>
			<div className="justify-self-end">{children}</div>
		</div>
	);
}

const hexColorPattern = /^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$/;

function ColorControl({
	onChange,
	presetColor,
	value,
}: {
	onChange: (value: string) => void;
	presetColor: string;
	value: string;
}) {
	const color = normalizeHexColor(value || presetColor);
	const [text, setText] = useState(color);
	const displayColor = hexColorPattern.test(text)
		? normalizeHexColor(text)
		: color;

	useEffect(() => {
		setText(color);
	}, [color]);

	const commitText = (nextValue: string) => {
		setText(nextValue);
		if (hexColorPattern.test(nextValue)) {
			onChange(normalizeHexColor(nextValue));
		}
	};

	return (
		<div className="flex h-8 w-72 items-center gap-2 rounded-lg border bg-background/60 px-2">
			<label className="relative grid size-4 shrink-0 cursor-pointer place-items-center rounded-full border border-current/20">
				<span
					className="size-3 rounded-full"
					style={{ backgroundColor: displayColor }}
				/>
				<input
					aria-label="Choose color"
					className="absolute inset-0 cursor-pointer opacity-0"
					onChange={(event) => {
						commitText(event.currentTarget.value.toUpperCase());
					}}
					onInput={(event) => {
						commitText(event.currentTarget.value.toUpperCase());
					}}
					type="color"
					value={displayColor}
				/>
			</label>
			<input
				aria-label="Color code"
				className="h-full min-w-0 flex-1 bg-transparent text-sm outline-none"
				onBlur={() => {
					if (hexColorPattern.test(text)) {
						onChange(normalizeHexColor(text));
					} else {
						setText(color);
					}
				}}
				onChange={(event) => {
					commitText(event.currentTarget.value);
				}}
				onInput={(event) => {
					commitText(event.currentTarget.value);
				}}
				spellCheck={false}
				value={text}
			/>
		</div>
	);
}

function normalizeHexColor(color: string) {
	if (!hexColorPattern.test(color)) {
		return "#000000";
	}

	if (color.length === 4) {
		return `#${color[1]}${color[1]}${color[2]}${color[2]}${color[3]}${color[3]}`.toUpperCase();
	}

	return color.toUpperCase();
}

function ThemeSelect({
	dropdownBoundary,
	onValueChange,
	options,
	value,
}: {
	dropdownBoundary: Element | null;
	onValueChange: (value: string) => void;
	options: ThemeOption[];
	value: string;
}) {
	const selected = getThemeOption(value);
	return (
		<Select
			onValueChange={(nextValue) => {
				if (nextValue) {
					onValueChange(nextValue);
				}
			}}
			value={value}
		>
			<SelectTrigger className="w-72 bg-background/60">
				<SelectValue>
					<Swatch option={selected} />
					<span>{selected.label}</span>
				</SelectValue>
			</SelectTrigger>
			<SelectContent
				align="end"
				alignItemWithTrigger={false}
				className="max-h-80 w-72"
				collisionAvoidance={{
					align: "shift",
					fallbackAxisSide: "none",
					side: "flip",
				}}
				collisionBoundary={dropdownBoundary ?? undefined}
				collisionPadding={12}
			>
				{options.map((option) => (
					<SelectItem key={option.value} value={option.value}>
						<Swatch option={option} />
						<span>{option.label}</span>
					</SelectItem>
				))}
			</SelectContent>
		</Select>
	);
}

function FontSelect({
	dropdownBoundary,
	onValueChange,
	options,
	value,
}: {
	dropdownBoundary: Element | null;
	onValueChange: (value: string) => void;
	options: FontOption[];
	value: string;
}) {
	const selected = getFontOption(value);
	return (
		<Select
			onValueChange={(nextValue) => {
				if (nextValue) {
					onValueChange(nextValue);
				}
			}}
			value={value}
		>
			<SelectTrigger className="w-72 bg-background/60">
				<SelectValue>{selected.label}</SelectValue>
			</SelectTrigger>
			<SelectContent
				align="end"
				alignItemWithTrigger={false}
				className="max-h-80 w-72"
				collisionAvoidance={{
					align: "shift",
					fallbackAxisSide: "none",
					side: "flip",
				}}
				collisionBoundary={dropdownBoundary ?? undefined}
				collisionPadding={12}
			>
				{options.map((option) => (
					<SelectItem key={option.value} value={option.value}>
						<span style={{ fontFamily: option.stack }}>{option.label}</span>
					</SelectItem>
				))}
			</SelectContent>
		</Select>
	);
}

function Swatch({ option }: { option: ThemeOption }) {
	return (
		<span
			className="inline-flex size-6 items-center justify-center rounded-md font-semibold text-xs"
			style={{
				background: option.variables.background,
				color: option.variables.primary,
			}}
		>
			Aa
		</span>
	);
}
