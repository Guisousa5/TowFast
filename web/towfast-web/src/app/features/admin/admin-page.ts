import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { firstValueFrom, forkJoin } from 'rxjs';
import { TowFastApiService } from '../../core/towfast-api.service';
import { AddressPicker } from '../../shared/address-picker';
import * as M from '../../api/generated/models';
type Tab = 'clients' | 'providers' | 'services';
@Component({
  selector: 'app-admin-page',
  imports: [
    FormsModule,
    RouterLink,
    NzButtonModule,
    NzIconModule,
    NzInputModule,
    NzInputNumberModule,
    NzModalModule,
    NzSelectModule,
    AddressPicker,
  ],
  templateUrl: './admin-page.html',
  styleUrl: './admin-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminPage implements OnInit {
  private readonly api = inject(TowFastApiService);
  private readonly messages = inject(NzMessageService);
  readonly tab = signal<Tab>('clients');
  readonly clients = signal<M.ClientResponse[]>([]);
  readonly providers = signal<M.ProviderResponse[]>([]);
  readonly types = signal<M.ServiceTypeResponse[]>([]);
  readonly clientDialog = signal(false);
  readonly providerDialog = signal(false);
  readonly editingProviderId = signal<string | null>(null);
  readonly busy = signal(false);
  readonly apiHealth = signal<'checking' | 'online' | 'degraded'>('checking');
  name = '';
  email = '';
  password = '';
  phone = '';
  document = '';
  documentType: M.DocumentType = 'Cpf';
  legalName = '';
  tradeName = '';
  radius = 30;
  latitude?: number;
  longitude?: number;
  readonly documentTypes = [
    { label: 'CPF', value: 'Cpf' },
    { label: 'CNPJ', value: 'Cnpj' },
  ];
  ngOnInit() {
    this.load();
    forkJoin([this.api.health(), this.api.healthLive()]).subscribe({
      next: () => this.apiHealth.set('online'),
      error: () => this.apiHealth.set('degraded'),
    });
  }
  load() {
    forkJoin({
      clients: this.api.clients(),
      providers: this.api.providers(),
      types: this.api.serviceTypes(),
    }).subscribe((x) => {
      this.clients.set(x.clients);
      this.providers.set(x.providers);
      this.types.set(x.types);
    });
  }
  async createClient() {
    this.busy.set(true);
    try {
      await firstValueFrom(
        this.api.createClient({
          name: this.name,
          email: this.email,
          password: this.password,
          phone: this.phone,
          document: this.document,
          documentType: this.documentType,
        }),
      );
      this.clientDialog.set(false);
      this.ok('Cliente criado');
      this.load();
    } finally {
      this.busy.set(false);
    }
  }
  async toggleClient(c: M.ClientResponse) {
    if (!c.id) return;
    await firstValueFrom(
      this.api.updateClient(c.id, {
        name: c.name,
        phone: c.phone,
        notes: c.notes,
        isActive: !c.isActive,
      }),
    );
    this.load();
  }
  openProvider(provider?: M.ProviderResponse): void {
    this.editingProviderId.set(provider?.id ?? null);
    this.legalName = provider?.legalName ?? '';
    this.tradeName = provider?.tradeName ?? '';
    this.email = provider?.email ?? '';
    this.phone = provider?.phone ?? '';
    this.document = provider?.document ?? '';
    this.radius = provider?.serviceRadiusKm ?? 30;
    this.latitude = provider?.serviceCenterLatitude;
    this.longitude = provider?.serviceCenterLongitude;
    this.password = '';
    this.providerDialog.set(true);
  }
  setProviderCenter(location: M.LocationInput): void {
    this.latitude = location.latitude ?? undefined;
    this.longitude = location.longitude ?? undefined;
  }
  async saveProvider() {
    if (this.latitude == null || this.longitude == null) {
      this.messages.warning('Selecione o centro de atendimento no mapa.');
      return;
    }
    this.busy.set(true);
    try {
      const id = this.editingProviderId();
      if (id)
        await firstValueFrom(
          this.api.updateProvider(id, {
            legalName: this.legalName,
            tradeName: this.tradeName,
            email: this.email,
            phone: this.phone,
            serviceCenterLatitude: this.latitude,
            serviceCenterLongitude: this.longitude,
            serviceRadiusKm: this.radius,
          }),
        );
      else
        await firstValueFrom(
          this.api.createProvider({
            legalName: this.legalName,
            tradeName: this.tradeName,
            email: this.email,
            password: this.password,
            phone: this.phone,
            document: this.document,
            serviceCenterLatitude: this.latitude,
            serviceCenterLongitude: this.longitude,
            serviceRadiusKm: this.radius,
          }),
        );
      this.providerDialog.set(false);
      this.ok(id ? 'Prestador atualizado' : 'Prestador criado');
      this.load();
    } finally {
      this.busy.set(false);
    }
  }
  async toggleProvider(p: M.ProviderResponse) {
    if (!p.id) return;
    await firstValueFrom(this.api.setProviderActive(p.id, !p.isActive));
    this.load();
  }
  async toggleType(t: M.ServiceTypeResponse) {
    if (!t.id) return;
    await firstValueFrom(this.api.setServiceTypeActive(t.id, !t.isActive));
    this.load();
  }
  private ok(summary: string) {
    this.messages.success(summary);
  }
}
