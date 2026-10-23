import { chromium } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';

const browser = await chromium.launch({
  headless: true,
  executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
});
console.log('Browser iniciado.');
const output = new URL('../artifacts/visual/', import.meta.url);
await mkdir(output, { recursive: true });
const shot = (name) => fileURLToPath(new URL(name, output));

async function pageFor(user) {
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
  page.setDefaultTimeout(10000);
  await page.addInitScript((current) => {
    sessionStorage.setItem('towfast.refresh-token', 'refresh');
    sessionStorage.setItem('towfast.user', JSON.stringify(current));
  }, user);
  await page.route('**/config.json', (route) =>
    route.fulfill({
      json: {
        apiBaseUrl: '',
        googleMapsApiKey: '',
        googleMapsMapId: '',
        trackingIntervalMs: 10000,
      },
    }),
  );
  await page.route('**/api/v1/**', (route) => {
    const path = new URL(route.request().url()).pathname;
    const auth = { accessToken: 'access', refreshToken: 'refresh-2', user };
    if (path.endsWith('/auth/refresh')) return route.fulfill({ json: auth });
    if (path === '/api/v1/clients')
      return route.fulfill({
        json: [
          {
            id: 'client-1',
            name: 'Cliente Teste',
            email: 'cliente@teste.com',
            phone: '11999999999',
            document: '123',
            documentType: 'Cpf',
            isActive: true,
          },
        ],
      });
    if (path.includes('/clients/client-1/vehicles'))
      return route.fulfill({
        json: [
          {
            id: 'vehicle-1',
            clientId: 'client-1',
            manufacturer: 'Fiat',
            model: 'Argo',
            licensePlate: 'ABC1D23',
            isActive: true,
          },
        ],
      });
    if (path.includes('/clients/client-1/locations')) return route.fulfill({ json: [] });
    if (path === '/api/v1/providers')
      return route.fulfill({
        json: [
          {
            id: 'provider-1',
            legalName: 'Reboques Centro Ltda',
            tradeName: 'Reboques Centro',
            document: '123',
            email: 'p@teste.com',
            phone: '11',
            serviceCenterLatitude: -23.5,
            serviceCenterLongitude: -46.6,
            serviceRadiusKm: 30,
            isActive: true,
          },
        ],
      });
    if (path === '/api/v1/service-types')
      return route.fulfill({
        json: [
          { id: 'tow', code: 'Tow', name: 'Reboque', requiresDestination: true, isActive: true },
        ],
      });
    if (path === '/api/v1/service-requests/available')
      return route.fulfill({ json: { items: [], page: 1, pageSize: 20, total: 0, totalPages: 1 } });
    if (path === '/api/v1/service-requests')
      return route.fulfill({ json: { items: [], page: 1, pageSize: 20, total: 0, totalPages: 1 } });
    return route.fulfill({ json: [] });
  });
  return page;
}

const admin = await pageFor({ id: 'admin-1', email: 'admin@towfast.com', role: 'Admin' });
await admin.goto('http://127.0.0.1:4200/app/gestao', { waitUntil: 'domcontentloaded' });
console.log('Gestão carregada.');
await admin.getByRole('button', { name: /Prestadores/ }).click();
await admin.getByRole('button', { name: 'Novo prestador' }).click();
console.log(
  `Modais: ${await admin.locator('.ant-modal').count()}, seletores: ${await admin.locator('app-address-picker').count()}`,
);
await admin.locator('app-address-picker').waitFor();
if ((await admin.getByText(/^Latitude$/).count()) || (await admin.getByText(/^Longitude$/).count()))
  throw new Error('Coordenadas manuais ainda aparecem no prestador.');
await admin.screenshot({ path: shot('provider-modal.png'), fullPage: true });

const client = await pageFor({
  id: 'user-1',
  email: 'cliente@teste.com',
  role: 'Client',
  clientId: 'client-1',
});
await client.goto('http://127.0.0.1:4200/app/atendimentos', { waitUntil: 'domcontentloaded' });
console.log('Atendimentos carregados.');
await client.getByRole('button', { name: 'Novo chamado' }).click();
await client.locator('nz-modal-container nz-select').first().click();
await client.locator('.ant-select-item-option').filter({ hasText: 'Reboque' }).click();
const addressPickers = client.locator('app-address-picker');
await addressPickers.nth(1).waitFor();
const addressPickerCount = await addressPickers.count();
console.log(`Endereços no chamado: ${addressPickerCount}`);
if (addressPickerCount !== 2)
  throw new Error(`Esperados 2 seletores de endereço, encontrados ${addressPickerCount}.`);
if (
  (await client.getByText(/^Latitude$/).count()) ||
  (await client.getByText(/^Longitude$/).count())
)
  throw new Error('Coordenadas manuais ainda aparecem no chamado.');
await client.screenshot({ path: shot('tow-request-modal.png'), fullPage: true });

console.log(
  'Visual check passed: gestão de prestador e chamado de reboque com dois seletores de endereço.',
);
await browser.close();
