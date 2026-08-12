import { copyFile, mkdir } from "node:fs/promises";
import { resolve } from "node:path";

const projectRoot = resolve(import.meta.dirname, "..");
const sourceRoot = resolve(projectRoot, "node_modules", "@tabler", "core", "dist");
const destinationRoot = resolve(
  projectRoot,
  "Temperatura.Web",
  "wwwroot",
  "lib",
  "tabler"
);

await mkdir(resolve(destinationRoot, "css"), { recursive: true });
await mkdir(resolve(destinationRoot, "js"), { recursive: true });

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
  )
]);

console.log("Recursos de Tabler copiados a wwwroot/lib/tabler.");
