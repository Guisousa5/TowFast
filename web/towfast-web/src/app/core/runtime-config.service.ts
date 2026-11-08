import { Injectable } from '@angular/core';

export interface RuntimeConfig { apiBaseUrl: string; googleMapsApiKey: string; googleMapsMapId: string; trackingIntervalMs: number; }

@Injectable({ providedIn: 'root' })
export class RuntimeConfigService {
  static config: RuntimeConfig = { apiBaseUrl: '', googleMapsApiKey: '', googleMapsMapId: '', trackingIntervalMs: 10000 };

  static async load(): Promise<void> {
    try {
      const shared = await RuntimeConfigService.read('/config.json');
      const local = await RuntimeConfigService.read('/config.local.json');
      RuntimeConfigService.config = { ...RuntimeConfigService.config, ...shared, ...local };
    } catch { /* Defaults support the local reverse proxy. */ }
  }

  private static async read(url: string): Promise<Partial<RuntimeConfig>> {
    const response = await fetch(url, { cache: 'no-store' });
    return response.ok ? await response.json() as Partial<RuntimeConfig> : {};
  }

  get value(): RuntimeConfig { return RuntimeConfigService.config; }
  get apiBaseUrl(): string { return RuntimeConfigService.config.apiBaseUrl.replace(/\/$/, ''); }
}
