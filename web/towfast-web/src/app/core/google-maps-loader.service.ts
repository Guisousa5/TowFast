import { Injectable, inject } from '@angular/core';
import { importLibrary, setOptions } from '@googlemaps/js-api-loader';
import { RuntimeConfigService } from './runtime-config.service';

@Injectable({ providedIn: 'root' })
export class GoogleMapsLoaderService {
  private readonly config = inject(RuntimeConfigService);
  private configured = false;

  get available(): boolean {
    return !!this.config.value.googleMapsApiKey;
  }

  private configure(): void {
    if (this.configured) return;
    setOptions({
      key: this.config.value.googleMapsApiKey,
      v: 'weekly',
      language: 'pt-BR',
      region: 'BR',
    });
    this.configured = true;
  }

  async maps(): Promise<google.maps.MapsLibrary> {
    this.configure();
    return importLibrary('maps');
  }
  async marker(): Promise<google.maps.MarkerLibrary> {
    this.configure();
    return importLibrary('marker');
  }
  async places(): Promise<google.maps.PlacesLibrary> {
    this.configure();
    return importLibrary('places');
  }
  async geocoding(): Promise<google.maps.GeocodingLibrary> {
    this.configure();
    return importLibrary('geocoding');
  }
}
