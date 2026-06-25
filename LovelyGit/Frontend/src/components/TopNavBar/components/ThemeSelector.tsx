import { Moon, Sun, SunMoon } from "lucide-react";
import { useEffect, useState } from "react";
import useSystemTheme from "react-use-system-theme";
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuGroup,
	DropdownMenuLabel,
	DropdownMenuRadioGroup,
	DropdownMenuRadioItem,
	DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import type { AppTheme } from "@/generated/types";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import { calculateTheme } from "@/lib/settings/theme/themeUtils";

const themeOptions: Array<{
	icon: typeof Sun;
	label: string;
	value: AppTheme;
}> = [
	{ icon: Sun, label: "Light", value: "Light" },
	{ icon: Moon, label: "Dark", value: "Dark" },
	{ icon: SunMoon, label: "System", value: "System" },
];

export function ThemeSelector() {
	const theme = useSetting("Theme");
	const systemTheme = useSystemTheme("dark");
	const [systemPreference, setSystemPreference] = useState<
		"dark" | "light" | null
	>(systemTheme);
	const [open, setOpen] = useState(false);
	const Icon =
		themeOptions.find((option) => option.value === theme)?.icon ?? SunMoon;

	useEffect(() => {
		document.documentElement.classList.toggle(
			"dark",
			calculateTheme(theme, systemPreference) === "Dark",
		);
	}, [theme, systemPreference]);

	useEffect(() => {
		const mediaQuery = window.matchMedia?.("(prefers-color-scheme: dark)");
		if (!mediaQuery) {
			return;
		}

		const onChange = () => {
			setSystemPreference(mediaQuery.matches ? "dark" : "light");
		};

		mediaQuery.addEventListener("change", onChange);
		onChange();
		return () => {
			mediaQuery.removeEventListener("change", onChange);
		};
	}, []);

	return (
		<DropdownMenu open={open} onOpenChange={setOpen}>
			<DropdownMenuTrigger
				className="inline-flex size-7 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
				aria-label="Select colour theme"
			>
				<Icon className="size-4" />
			</DropdownMenuTrigger>
			<DropdownMenuContent className="min-w-44" align="end">
				<DropdownMenuGroup>
					<DropdownMenuLabel>Select Colour Theme</DropdownMenuLabel>
					<DropdownMenuRadioGroup
						value={theme}
						onValueChange={(value) => {
							void setSetting("Theme", value as AppTheme);
							setOpen(false);
						}}
					>
						{themeOptions.map((option) => (
							<DropdownMenuRadioItem value={option.value} key={option.value}>
								<option.icon className="size-4" />
								{option.label}
							</DropdownMenuRadioItem>
						))}
					</DropdownMenuRadioGroup>
				</DropdownMenuGroup>
			</DropdownMenuContent>
		</DropdownMenu>
	);
}
