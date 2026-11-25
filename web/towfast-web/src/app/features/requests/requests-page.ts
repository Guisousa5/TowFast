import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { Subscription, finalize, firstValueFrom, forkJoin, timer } from 'rxjs';
import { TowFastApiService } from '../../core/towfast-api.service';
import { SessionService } from '../../core/session.service';
import { RuntimeConfigService } from '../../core/runtime-config.service';
import { TrackingMap } from '../../shared/tracking-map';
import { AddressPicker } from '../../shared/address-picker';
import { buildCreateRequestCommand } from './service-request-command';
import { canTrackStatus, trackingIntervalMs } from './tracking-policy';
import * as M from '../../api/generated/models';

@Component({
  selector: 'app-requests-page',
  imports: [
    FormsModule,
    DatePipe,
    NzButtonModule,
    NzIconModule,
    NzInputModule,
    NzModalModule,
    NzSelectModule,
    NzTagModule,
    TrackingMap,
    AddressPicker,
  ],
  templateUrl: './requests-page.html',
  styleUrl: './requests-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestsPage implements OnInit {
  private readonly api = inject(TowFastApiService);
  readonly session = inject(SessionService);
  private readonly messages = inject(NzMessageService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly config = inject(RuntimeConfigService);
  private readonly destroy = inject(DestroyRef);
  readonly loading = signal(true);
  readonly items = signal<M.ServiceRequestResponse[]>([]);
  readonly available = signal<M.AvailableRequestResponse[]>([]);
  readonly detail = signal<M.ServiceRequestDetailResponse | null>(null);
  readonly tracking = signal<M.TrackingResponse | null>(null);
  readonly vehicles = signal<M.VehicleResponse[]>([]);
  readonly providerVehicles = signal<M.ProviderVehicleResponse[]>([]);
  readonly locations = signal<M.LocationResponse[]>([]);
  readonly types = signal<M.ServiceTypeResponse[]>([]);
  readonly detailVisible = signal(false);
  readonly createVisible = signal(false);
  readonly completeVisible = signal(false);
  readonly busy = signal(false);
  readonly priceQuote = signal<M.PriceQuoteResponse | null>(null);
  private quotedCommand: string | null = null;
  readonly sharingLocation = signal(false);
  readonly providerSide = computed(() => this.session.role() === 'Driver');
  readonly canOperate = computed(() =>
    ['Admin', 'Operator', 'Provider', 'Driver'].includes(this.session.role() ?? ''),
  );
  readonly hasActive = computed(() =>
    this.items().some((x) => ['Accepted', 'EnRoute', 'InProgress'].includes(x.status ?? '')),
  );
  readonly canCreate = computed(() => this.session.role() === 'Client');
  readonly requiresDestination = () => {
    const type = this.types().find((x) => x.id === this.serviceTypeId);
    return type?.code === 'Tow' || type?.requiresDestination === true;
  };
  readonly page = signal(1);
  readonly totalPages = signal(1);
  filterStatus = '';
  filterFrom = '';
  filterTo = '';
  filterServiceTypeId = '';
  filterClientId = '';
  filterProviderId = '';
  filterDriverId = '';
  readonly statuses = [
    { label: 'Aguardando', value: 'Pending' },
    { label: 'Aceito', value: 'Accepted' },
    { label: 'A caminho', value: 'EnRoute' },
    { label: 'Em atendimento', value: 'InProgress' },
    { label: 'Concluído', value: 'Completed' },
    { label: 'Cancelado', value: 'Cancelled' },
  ];
  serviceTypeId = '';
  clientVehicleId = '';
  originLocationId = '';
  destinationLocationId = '';
  customerNotes = '';
  originSelection: M.LocationInput | null = null;
  destinationSelection: M.LocationInput | null = null;
  acceptVehicleId = '';
  completionNotes = '';
  itemName = '';
  itemDescription = '';
  itemUnit = 'un';
  itemQuantity = 1;
  readonly completionItems = signal<M.CompleteItemCommand[]>([]);

  private watchId?: number;
  private lastPublished = 0;
  private locationPublishSubscription?: Subscription;
  private trackingPollSubscription?: Subscription;
  ngOnInit(): void {
    const q = this.route.snapshot.queryParamMap;
    this.page.set(Number(q.get('page')) || 1);
    this.filterStatus = q.get('status') ?? '';
    this.filterFrom = q.get('from') ?? '';
    this.filterTo = q.get('to') ?? '';
    this.filterServiceTypeId = q.get('serviceTypeId') ?? '';
    this.filterClientId = q.get('clientId') ?? '';
    this.filterProviderId = q.get('providerId') ?? '';
    this.filterDriverId = q.get('driverId') ?? '';
    this.reload();
    const listPoll = timer(15000, 15000).subscribe(() => {
      if (!document.hidden && !this.createVisible() && !this.detailVisible() && !this.busy()) this.reload();
    });
    const id = q.get('id');
    if (id) void this.openDetail(id);
    const sub = timer(0, trackingIntervalMs(this.config.value.trackingIntervalMs)).subscribe(() =>
      this.refreshTracking(),
    );
    this.destroy.onDestroy(() => {
      listPoll.unsubscribe();
      sub.unsubscribe();
      this.closeDetail();
    });
  }
  reload(): void {
    this.loading.set(true);
    const user = this.session.user();
    const common = {
      page: this.page(),
      pageSize: 20,
      status: this.filterStatus,
      from: this.filterFrom ? new Date(this.filterFrom).toISOString() : undefined,
      to: this.filterTo ? new Date(this.filterTo).toISOString() : undefined,
      serviceTypeId: this.filterServiceTypeId,
      clientId: this.filterClientId,
      providerId: this.filterProviderId,
      driverId: this.filterDriverId,
    };
    if (this.providerSide())
      forkJoin({
        requests: this.api.requests(common),
        available: this.api.availableRequests({ page: 1, pageSize: 20 }),
        types: this.api.serviceTypes(),
      }).subscribe({
        next: (r) => {
          this.items.set(r.requests.items ?? []);
          this.totalPages.set(r.requests.totalPages ?? 1);
          this.available.set(r.available.items ?? []);
          this.types.set(r.types.filter((x) => x.isActive));
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
    else
      forkJoin({ requests: this.api.requests(common), types: this.api.serviceTypes() }).subscribe({
        next: (r) => {
          this.items.set(r.requests.items ?? []);
          this.totalPages.set(r.requests.totalPages ?? 1);
          this.types.set(r.types.filter((x) => x.isActive));
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
    if (user?.clientId)
      forkJoin({
        vehicles: this.api.clientVehicles(user.clientId),
        locations: this.api.clientLocations(user.clientId),
      }).subscribe((r) => {
        this.vehicles.set(r.vehicles.filter((x) => x.isActive));
        this.locations.set(r.locations);
      });
    if (user?.providerId)
      this.api
        .providerVehicles(user.providerId)
        .subscribe((x) => this.providerVehicles.set(x.filter((v) => v.isActive)));
  }
  applyFilters(page = 1): void {
    this.page.set(page);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        page,
        status: this.filterStatus || null,
        from: this.filterFrom || null,
        to: this.filterTo || null,
        serviceTypeId: this.filterServiceTypeId || null,
        clientId: this.filterClientId || null,
        providerId: this.filterProviderId || null,
        driverId: this.filterDriverId || null,
      },
      queryParamsHandling: 'merge',
    });
    this.reload();
  }
  async openDetail(publicId?: string | null): Promise<void> {
    if (!publicId) return;
    this.closeDetail();
    this.detailVisible.set(true);
    this.detail.set(await firstValueFrom(this.api.request(publicId)));
    this.refreshTracking();
  }
  closeDetail(): void {
    this.stopLocation();
    this.trackingPollSubscription?.unsubscribe();
    this.trackingPollSubscription = undefined;
    this.tracking.set(null);
    this.detailVisible.set(false);
  }
  async create(): Promise<void> {
    const serviceType = this.types().find((x) => x.id === this.serviceTypeId);
    if (!serviceType) {
      this.messages.warning('Selecione o tipo de serviço.');
      return;
    }
    let body: M.CreateServiceRequestCommand;
    try {
      body = buildCreateRequestCommand({
        serviceType,
        clientVehicleId: this.clientVehicleId,
        originLocationId: this.originLocationId,
        origin: this.originSelection,
        destinationLocationId: this.destinationLocationId,
        destination: this.destinationSelection,
        customerNotes: this.customerNotes,
      });
    } catch (error) {
      this.messages.warning(error instanceof Error ? error.message : 'Revise os dados do chamado.');
      return;
    }
    this.busy.set(true);
    try {
      const command = JSON.stringify(body);
      if (!this.priceQuote() || this.quotedCommand !== command) {
        const quote = await firstValueFrom(this.api.quoteRequest(body));
        this.priceQuote.set(quote);
        this.quotedCommand = command;
        return;
      }
      await firstValueFrom(this.api.createRequest({ ...body, expectedPrice: this.priceQuote()!.price }));
      this.messages.success('Chamado aberto: a solicitação já está visível para os prestadores.');
      this.createVisible.set(false);
      this.resetCreate();
      this.reload();
    } catch {
      this.priceQuote.set(null);
      this.quotedCommand = null;
    } finally {
      this.busy.set(false);
    }
  }
  async accept(item: M.AvailableRequestResponse): Promise<void> {
    if (this.hasActive()) {
      this.messages.warning(
        'Atendimento em andamento: conclua ou libere o atendimento atual antes de aceitar outro.',
      );
      return;
    }
    if (!this.acceptVehicleId) {
      this.messages.warning('Selecione um veículo da frota');
      return;
    }
    this.busy.set(true);
    try {
      await firstValueFrom(
        this.api.acceptRequest(item.id!, {
          providerVehicleId: this.acceptVehicleId,
          version: item.version,
        }),
      );
      this.messages.success('Atendimento aceito');
      this.reload();
      await this.openDetail(item.publicId);
    } catch {
      this.reload();
    } finally {
      this.busy.set(false);
    }
  }
  async transition(action: 'release' | 'enroute' | 'start' | 'complete' | 'cancel'): Promise<void> {
    const request = this.detail()?.request;
    if (!request?.id) return;
    this.busy.set(true);
    try {
      if (action === 'release')
        await firstValueFrom(
          this.api.releaseRequest(request.id, request.version, 'Liberado pelo prestador'),
        );
      if (action === 'enroute') {
        const updated = await firstValueFrom(
          this.api.enRouteRequest(request.id, request.version),
        );
        this.detail.update((current) => (current ? { ...current, request: updated } : current));
        this.messages.success('Deslocamento iniciado');
        this.reload();
        this.toggleLocation();
        return;
      }
      if (action === 'start')
        await firstValueFrom(this.api.startRequest(request.id, request.version));
      if (action === 'complete')
        await firstValueFrom(
          this.api.completeRequest(request.id, {
            version: request.version,
            completionNotes: 'Atendimento concluído',
          }),
        );
      if (action === 'cancel')
        await firstValueFrom(
          this.api.cancelRequest(request.id, request.version, 'Cancelado pelo usuário'),
        );
      this.messages.success('Atendimento atualizado');
      this.closeDetail();
      this.reload();
    } catch {
      this.closeDetail();
      this.reload();
    } finally {
      this.busy.set(false);
    }
  }
  toggleLocation(): void {
    if (this.sharingLocation()) {
      this.stopLocation();
      return;
    }

    const request = this.detail()?.request;
    if (!request?.id || !canTrackStatus(request.status)) {
      this.messages.warning('Inicie o deslocamento antes de compartilhar a localização.');
      return;
    }
    if (!navigator.geolocation) {
      this.messages.error('Este navegador não oferece acesso à localização.');
      return;
    }

    const requestId = request.id;
    const interval = trackingIntervalMs(this.config.value.trackingIntervalMs);
    this.lastPublished = 0;
    this.sharingLocation.set(true);
    this.watchId = navigator.geolocation.watchPosition(
      (position) => {
        const currentRequest = this.detail()?.request;
        if (
          currentRequest?.id !== requestId ||
          !canTrackStatus(currentRequest.status) ||
          !this.detailVisible()
        ) {
          this.stopLocation();
          return;
        }

        const now = Date.now();
        if (
          now - this.lastPublished < interval ||
          (this.locationPublishSubscription && !this.locationPublishSubscription.closed)
        ) {
          return;
        }

        this.lastPublished = now;
        const c = position.coords;
        this.locationPublishSubscription = this.api
          .publishPosition(requestId, {
            latitude: c.latitude,
            longitude: c.longitude,
            accuracyMeters: c.accuracy,
            headingDegrees: c.heading,
            speedKmh: c.speed === null ? null : c.speed * 3.6,
            capturedAt: new Date(position.timestamp).toISOString(),
            sourceEventId: crypto.randomUUID(),
          })
          .pipe(finalize(() => (this.locationPublishSubscription = undefined)))
          .subscribe({
            next: (published) => this.tracking.set(published),
            error: () => {
              this.stopLocation();
              this.messages.warning(
                'O compartilhamento foi pausado porque a posição não pôde ser atualizada.',
              );
            },
          });
      },
      () => {
        if (!this.sharingLocation()) return;
        this.stopLocation();
        this.messages.error(
          'Localização indisponível: permita o acesso à localização para compartilhar o trajeto.',
        );
      },
      { enableHighAccuracy: true, maximumAge: 5000, timeout: 15000 },
    );
  }
  addCompletionItem(): void {
    if (!this.itemName || this.itemQuantity <= 0) return;
    this.completionItems.update((x) => [
      ...x,
      {
        name: this.itemName,
        description: this.itemDescription,
        unit: this.itemUnit,
        quantity: this.itemQuantity,
      },
    ]);
    this.itemName = '';
    this.itemDescription = '';
    this.itemQuantity = 1;
  }
  async complete(): Promise<void> {
    const request = this.detail()?.request;
    if (!request?.id) return;
    this.busy.set(true);
    try {
      await firstValueFrom(
        this.api.completeRequest(request.id, {
          version: request.version,
          completionNotes: this.completionNotes,
          items: this.completionItems(),
        }),
      );
      this.stopLocation();
      this.completeVisible.set(false);
      this.closeDetail();
      this.messages.success('Atendimento concluído');
      this.reload();
    } finally {
      this.busy.set(false);
    }
  }
  private stopLocation(): void {
    if (this.watchId !== undefined && navigator.geolocation) {
      navigator.geolocation.clearWatch(this.watchId);
    }
    this.watchId = undefined;
    this.locationPublishSubscription?.unsubscribe();
    this.locationPublishSubscription = undefined;
    this.lastPublished = 0;
    this.sharingLocation.set(false);
  }
  private refreshTracking(): void {
    const request = this.detail()?.request;
    if (
      document.hidden ||
      !this.detailVisible() ||
      !request?.id ||
      !canTrackStatus(request.status)
    ) {
      if (this.sharingLocation() && !canTrackStatus(request?.status)) this.stopLocation();
      return;
    }
    if (this.trackingPollSubscription && !this.trackingPollSubscription.closed) return;

    const requestId = request.id;
    this.trackingPollSubscription = this.api
      .latestPosition(requestId)
      .pipe(finalize(() => (this.trackingPollSubscription = undefined)))
      .subscribe({
        next: (position) => {
          if (!this.detailVisible() || this.detail()?.request?.id !== requestId) return;
          const current = this.tracking()?.capturedAt;
          if (
            !current ||
            !position.capturedAt ||
            new Date(position.capturedAt) >= new Date(current)
          ) {
            this.tracking.set(position);
          }
        },
        error: () => {
          if (this.detailVisible() && this.detail()?.request?.id === requestId) {
            this.tracking.set(null);
          }
        },
      });
  }
  statusLabel(s?: string) {
    return (
      (
        {
          Pending: 'Aguardando',
          Accepted: 'Aceito',
          EnRoute: 'A caminho',
          InProgress: 'Em atendimento',
          Completed: 'Concluído',
          Cancelled: 'Cancelado',
        } as Record<string, string>
      )[s ?? ''] ?? s
    );
  }
  statusColor(s?: string): string {
    return s === 'Completed'
      ? 'success'
      : s === 'Cancelled'
        ? 'error'
        : s === 'Pending'
          ? 'warning'
          : s === 'InProgress'
            ? 'processing'
            : 'default';
  }
  typeName(id?: string) {
    return this.types().find((x) => x.id === id)?.name ?? 'Assistência';
  }
  formatPrice(value?: number | null): string {
    return value == null
      ? 'Preço não registrado'
      : new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
  }
  closeCreate(): void {
    this.createVisible.set(false);
    this.resetCreate();
  }
  invalidateQuote(): void {
    this.priceQuote.set(null);
    this.quotedCommand = null;
  }
  private resetCreate() {
    this.priceQuote.set(null);
    this.quotedCommand = null;
    this.serviceTypeId = '';
    this.clientVehicleId = '';
    this.originLocationId = '';
    this.destinationLocationId = '';
    this.customerNotes = '';
    this.originSelection = null;
    this.destinationSelection = null;
  }
}
