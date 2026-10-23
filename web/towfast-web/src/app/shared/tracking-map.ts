import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, Input, OnChanges, ViewChild, inject, signal } from '@angular/core';
import { GoogleMapsLoaderService } from '../core/google-maps-loader.service';
import { NzIconModule } from 'ng-zorro-antd/icon';

type Point = { latitude?: number | null; longitude?: number | null } | null | undefined;

@Component({
  selector: 'app-tracking-map',
  imports: [NzIconModule],
  template: `<div class="map-shell">@if(!configured()){<div class="map-empty"><nz-icon nzType="global" /><strong>Mapa indisponível</strong><span>Os dados textuais do atendimento continuam disponíveis.</span></div>}<div #canvas class="map" [class.hidden]="!configured()"></div></div>`,
  styles: [`.map-shell,.map{width:100%;height:100%;min-height:300px}.map-shell{position:relative;background:var(--surface-100);overflow:hidden;border-radius:6px}.hidden{display:none}.map-empty{height:100%;min-height:300px;display:grid;place-content:center;justify-items:center;gap:8px;color:var(--text-color-secondary);text-align:center;padding:24px}.map-empty nz-icon{font-size:2rem;color:#20a874}.map-empty span{font-size:.8rem}`],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TrackingMap implements AfterViewInit, OnChanges {
  @Input() origin?: Point; @Input() destination?: Point; @Input() driver?: Point; @Input() driverStale = false;
  @ViewChild('canvas') canvas?: ElementRef<HTMLDivElement>;
  private readonly loader = inject(GoogleMapsLoaderService);
  readonly configured = signal(this.loader.available);
  private map?: google.maps.Map;
  private markers = new Map<string, google.maps.Marker>();

  async ngAfterViewInit(): Promise<void> {
    if (!this.configured() || !this.canvas) return;
    try {
      const maps = await this.loader.maps();
      const center = this.valid(this.driver) ?? this.valid(this.origin) ?? { lat: -23.5505, lng: -46.6333 };
      this.map = new maps.Map(this.canvas.nativeElement, { center, zoom: 15, mapTypeControl: false, streetViewControl: false, fullscreenControl: false });
      await this.loader.marker();
      this.renderMarkers(true);
    } catch { this.configured.set(false); }
  }

  ngOnChanges(): void { this.renderMarkers(false); }

  private renderMarkers(fit: boolean): void {
    if (!this.map || typeof google === 'undefined') return;
    const points = [
      { key: 'origin', point: this.valid(this.origin), title: 'Origem' },
      { key: 'destination', point: this.valid(this.destination), title: 'Destino' },
      { key: 'driver', point: this.valid(this.driver), title: this.driverStale ? 'Motorista · posição desatualizada' : 'Motorista · última posição' },
    ];
    const bounds = new google.maps.LatLngBounds();
    for (const item of points) {
      const marker = this.markers.get(item.key);
      if (!item.point) { marker?.setMap(null); this.markers.delete(item.key); continue; }
      bounds.extend(item.point);
      if (marker) { marker.setPosition(item.point); marker.setTitle(item.title); }
      else this.markers.set(item.key, new google.maps.Marker({ position: item.point, map: this.map, title: item.title, opacity: item.key === 'driver' && this.driverStale ? .55 : 1 }));
    }
    if (fit && !bounds.isEmpty()) { this.map.fitBounds(bounds, 56); if (points.filter(x => x.point).length === 1) this.map.setZoom(15); }
  }

  private valid(point: Point): google.maps.LatLngLiteral | null {
    return point?.latitude != null && point?.longitude != null ? { lat: point.latitude, lng: point.longitude } : null;
  }
}
