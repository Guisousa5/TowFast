import { Injectable } from '@angular/core';

export interface RuntimeConfig { apiBaseUrl: string; googleMapsApiKey: string; googleMapsMapId: string; trackingIntervalMs: number; }

@Injectable({ providedIn: 'root' })
export class RuntimeConfigService {
  static config: RuntimeConfig = { apiBaseUrl: '', googleMapsApiKey: '', googleMapsMapId: '', trackingIntervalMs: 10000 };

  static async load(): Promise<void> {
    try {
      const response = await fetch('/config.json', { cache: 'no-store' });
      if (!response.ok) throw new Error('Runtime config unavailable');
      const loaded = await response.json() as Partial<RuntimeConfig>;
      RuntimeConfigService.config = { ...RuntimeConfigService.config, ...loaded };
    } catch { /* Defaults support the local reverse proxy. */ }
  }

  get value(): RuntimeConfig { return RuntimeConfigService.config; }
  get apiBaseUrl(): string { return RuntimeConfigService.config.apiBaseUrl.replace(/\/$/, ''); }
}
