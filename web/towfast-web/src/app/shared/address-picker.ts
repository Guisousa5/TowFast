import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { LocationInput } from '../api/generated/models/location-input';
import { GoogleMapsLoaderService } from '../core/google-maps-loader.service';

@Component({
  selector: 'app-address-picker',
  imports: [NzButtonModule, NzIconModule, NzInputModule],
  template: `
    <div class="picker">
      <div class="search-row">
        <input #search nz-input [placeholder]="placeholder" autocomplete="off" />
        <button
          nz-button
          type="button"
          aria-label="Usar minha localização"
          (click)="useCurrentLocation()"
          [nzLoading]="locating()"
        >
          <nz-icon nzType="environment" />
        </button>
      </div>
      @if (maps.available) {
        <div #canvas class="map" aria-label="Selecione o local no mapa"></div>
      } @else {
        <div class="fallback">
          Configure a chave Google Maps para buscar e selecionar endereços.
        </div>
      }
      @if (selected()) {
        <div class="selected">
          <nz-icon nzType="environment" /><span
            ><strong>{{ selected()?.label }}</strong
            ><small
              >{{ selected()?.street }}{{ selected()?.number ? ', ' + selected()?.number : '' }} ·
              {{ selected()?.city }}/{{ selected()?.state }}</small
            ></span
          >
        </div>
      }
    </div>
  `,
  styles: [
    `
      .picker {
        display: grid;
        gap: 10px;
      }
      .search-row {
        display: grid;
        grid-template-columns: 1fr 40px;
        gap: 8px;
      }
      .map {
        height: 250px;
        border: 1px solid var(--surface-border);
        border-radius: 6px;
        background: var(--surface-100);
      }
      .fallback {
        min-height: 90px;
        display: grid;
        place-items: center;
        padding: 16px;
        border: 1px dashed var(--surface-border);
        border-radius: 6px;
        color: var(--text-color-secondary);
        text-align: center;
      }
      .selected {
        display: flex;
        gap: 9px;
        align-items: start;
        padding: 10px 12px;
        background: var(--surface-50);
        border-radius: 5px;
      }
      .selected > nz-icon {
        color: #087a50;
        margin-top: 3px;
      }
      .selected span {
        display: grid;
      }
      .selected small {
        color: var(--text-color-secondary);
        font-weight: 400;
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AddressPicker implements AfterViewInit {
  @Input() placeholder = 'Busque um endereço';
  @Input() label = 'Local selecionado';
  @Input() initialLatitude?: number | null;
  @Input() initialLongitude?: number | null;
  @Output() readonly locationChange = new EventEmitter<LocationInput>();
  @ViewChild('canvas') canvas?: ElementRef<HTMLDivElement>;
  @ViewChild('search') search?: ElementRef<HTMLInputElement>;

  readonly maps = inject(GoogleMapsLoaderService);
  readonly locating = signal(false);
  readonly selected = signal<LocationInput | null>(null);
  private map?: google.maps.Map;
  private marker?: google.maps.Marker;
  private geocoder?: google.maps.Geocoder;

  async ngAfterViewInit(): Promise<void> {
    if (!this.maps.available || !this.canvas || !this.search) return;
    try {
      const [maps, marker, places, geocoding] = await Promise.all([
        this.maps.maps(),
        this.maps.marker(),
        this.maps.places(),
        this.maps.geocoding(),
      ]);
      const center =
        this.initialLatitude != null && this.initialLongitude != null
          ? { lat: this.initialLatitude, lng: this.initialLongitude }
          : { lat: -23.5505, lng: -46.6333 };
      this.map = new maps.Map(this.canvas.nativeElement, {
        center,
        zoom: this.initialLatitude != null ? 16 : 11,
        mapTypeControl: false,
        streetViewControl: false,
        fullscreenControl: false,
      });
      this.marker = new marker.Marker({
        map: this.map,
        position: this.initialLatitude != null ? center : undefined,
      });
      this.geocoder = new geocoding.Geocoder();
      this.map.addListener('click', (event: google.maps.MapMouseEvent) => {
        if (event.latLng) void this.selectPoint(event.latLng);
      });
      const autocomplete = new places.Autocomplete(this.search.nativeElement, {
        fields: ['address_components', 'formatted_address', 'geometry', 'name'],
        componentRestrictions: { country: 'br' },
      });
      autocomplete.addListener('place_changed', () => {
        const place = autocomplete.getPlace();
        if (!place.geometry?.location) return;
        this.applySelection(
          place.geometry.location,
          place.address_components ?? [],
          place.formatted_address ?? place.name ?? this.label,
        );
      });
      if (this.initialLatitude != null && this.initialLongitude != null)
        await this.selectPoint(center);
    } catch {
      /* The textual fallback remains usable when Google rejects the key or quota. */
    }
  }

  useCurrentLocation(): void {
    if (!navigator.geolocation) return;
    this.locating.set(true);
    navigator.geolocation.getCurrentPosition(
      (position) => {
        this.locating.set(false);
        void this.selectPoint({ lat: position.coords.latitude, lng: position.coords.longitude });
      },
      () => this.locating.set(false),
      { enableHighAccuracy: true, timeout: 15000, maximumAge: 30000 },
    );
  }

  private async selectPoint(point: google.maps.LatLng | google.maps.LatLngLiteral): Promise<void> {
    const latLng = point instanceof google.maps.LatLng ? point : new google.maps.LatLng(point);
    this.map?.panTo(latLng);
    this.map?.setZoom(16);
    this.marker?.setPosition(latLng);
    if (!this.geocoder) return;
    const response = await this.geocoder.geocode({ location: latLng });
    const result = response.results[0];
    this.applySelection(
      latLng,
      result?.address_components ?? [],
      result?.formatted_address ?? this.label,
    );
  }

  private applySelection(
    point: google.maps.LatLng,
    components: google.maps.GeocoderAddressComponent[],
    formatted: string,
  ): void {
    const part = (type: string, short = false) => {
      const item = components.find((x) => x.types.includes(type));
      return item ? (short ? item.short_name : item.long_name) : '';
    };
    const location: LocationInput = {
      label: this.label,
      street: part('route') || formatted,
      number: part('street_number') || null,
      district: part('sublocality_level_1') || part('neighborhood') || null,
      city: part('administrative_area_level_2') || part('locality'),
      state: part('administrative_area_level_1', true),
      postalCode: part('postal_code') || null,
      latitude: point.lat(),
      longitude: point.lng(),
    };
    this.marker?.setPosition(point);
    this.map?.panTo(point);
    this.selected.set(location);
    this.locationChange.emit(location);
    if (this.search) this.search.nativeElement.value = formatted;
  }
}
