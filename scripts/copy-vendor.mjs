import { copyFile, mkdir } from "node:fs/promises";
import { resolve } from "node:path";

const projectRoot = resolve(import.meta.dirname, "..");
const sourceRoot = resolve(projectRoot, "node_modules", "@tabler", "core", "dist");
const chartSourceRoot = resolve(projectRoot, "node_modules", "chart.js", "dist");
const destinationRoot = resolve(
  projectRoot,
  "Temperatura.Web",
  "wwwroot",
  "lib",
  "tabler"
);

await mkdir(resolve(destinationRoot, "css"), { recursive: true });
await mkdir(resolve(destinationRoot, "js"), { recursive: true });
await mkdir(
  resolve(projectRoot, "Temperatura.Web", "wwwroot", "lib", "chartjs"),
  { recursive: true }
);

await Promise.all([
  copyFile(
    resolve(sourceRoot, "css", "tabler.min.css"),
    resolve(destinationRoot, "css", "tabler.min.css")
  ),
  copyFile(
    resolve(sourceRoot, "js", "tabler.min.js"),
    resolve(destinationRoot, "js", "tabler.min.js")
  ),
  copyFile(
    resolve(sourceRoot, "js", "tabler-theme.min.js"),
    resolve(destinationRoot, "js", "tabler-theme.min.js")
  ),
  copyFile(
    resolve(chartSourceRoot, "chart.umd.js"),
    resolve(
      projectRoot,
      "Temperatura.Web",
      "wwwroot",
      "lib",
      "chartjs",
      "chart.umd.js"
    )
  )
]);

console.log("Recursos de Tabler y Chart.js copiados a wwwroot/lib.");
