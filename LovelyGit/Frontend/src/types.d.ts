type Theme = "light" | "dark" | null;

declare module "react-use-system-theme" {
	function useSystemTheme(initialTheme?: Theme): Theme;

	export = useSystemTheme;
}
