import path from "node:path";
import react, { reactCompilerPreset } from "@vitejs/plugin-react";
import babel from "@rolldown/plugin-babel";
import { defineConfig } from "vitest/config";

export default defineConfig({
	plugins: [react(), babel({ presets: [reactCompilerPreset()] })],
	resolve: {
		alias: {
			"@": path.resolve(__dirname, "./src"),
		},
	},
	test: {
		coverage: {
			exclude: [
				"src/**/*.test.{ts,tsx}",
				"src/generated/**",
				"src/components/ui/**",
				"src/test/**",
			],
			include: ["src/**/*.{ts,tsx}"],
			provider: "v8",
			reporter: ["text", "json-summary", "html"],
			reportsDirectory: "../../artifacts/coverage/frontend",
			thresholds: {
				branches: 25,
				functions: 18,
				lines: 28,
				statements: 28,
			},
		},
		environment: "node",
		setupFiles: ["./src/test/setup.ts"],
	},
});
