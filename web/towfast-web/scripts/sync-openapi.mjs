import { readFile, writeFile } from 'node:fs/promises';

const target = new URL('../openapi/towfast-v1.json', import.meta.url);
const source = process.env.OPENAPI_URL ?? 'http://127.0.0.1:5211/swagger/v1/swagger.json';
const check = process.argv.includes('--check');
const response = await fetch(source);
if (!response.ok) throw new Error(`OpenAPI indisponível em ${source}: HTTP ${response.status}`);
const live = await response.json();

const sort = value => Array.isArray(value) ? value.map(sort) : value && typeof value === 'object'
  ? Object.fromEntries(Object.entries(value).sort(([a], [b]) => a.localeCompare(b)).map(([key, item]) => [key, sort(item)]))
  : value;

if (check) {
  const current = JSON.parse(await readFile(target, 'utf8'));
  if (JSON.stringify(sort(current)) !== JSON.stringify(sort(live))) {
    throw new Error('O snapshot OpenAPI está diferente da API. Execute npm run api:sync.');
  }
  console.log(`OpenAPI sincronizado: ${Object.keys(live.paths ?? {}).length} endpoints.`);
} else {
  await writeFile(target, `${JSON.stringify(live, null, 2)}\n`, 'utf8');
  console.log(`OpenAPI atualizado: ${Object.keys(live.paths ?? {}).length} endpoints.`);
}
