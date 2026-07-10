import type { ReactNode } from "react";
import { useEffect, useState } from "react";
import type { FontOption } from "@/lib/settings/font/fontUtils";
import type { ThemeOption } from "@/lib/settings/theme/themeCatalog";
import { FontSelect, ThemeSelect } from "./AppearanceSelects";

export function ThemePanel({
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
