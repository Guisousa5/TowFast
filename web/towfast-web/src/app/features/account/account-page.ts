import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';
import { firstValueFrom, forkJoin } from 'rxjs';
import { TowFastApiService } from '../../core/towfast-api.service';
import { SessionService } from '../../core/session.service';
import { AddressPicker } from '../../shared/address-picker';
import * as M from '../../api/generated/models';

@Component({
  selector: 'app-account-page',
  imports: [
    FormsModule,
    NzButtonModule,
    NzIconModule,
    NzInputModule,
    NzInputNumberModule,
    NzModalModule,
    NzTooltipModule,
    AddressPicker,
  ],
  templateUrl: './account-page.html',
  styleUrl: './account-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountPage implements OnInit {
  private readonly api = inject(TowFastApiService);
  private readonly session = inject(SessionService);
  private readonly route = inject(ActivatedRoute);
  private readonly messages = inject(NzMessageService);
  readonly client = signal<M.ClientResponse | null>(null);
  readonly vehicles = signal<M.VehicleResponse[]>([]);
  readonly locations = signal<M.LocationResponse[]>([]);
  readonly profileDialog = signal(false);
  readonly vehicleDialog = signal(false);
  readonly locationDialog = signal(false);
  readonly busy = signal(false);
  private clientId = '';
  private locationSelection: M.LocationInput | null = null;
  model = '';
  manufacturer = '';
  plate = '';
  color = '';
  modelYear?: number;
  label = '';
  profileName = '';
  profilePhone = '';
  profileNotes = '';

  ngOnInit(): void {
    const canSelectScope = ['Admin', 'Operator'].includes(this.session.role() ?? '');
    this.clientId =
      (canSelectScope ? this.route.snapshot.queryParamMap.get('clientId') : null) ??
      this.session.user()?.clientId ??
      '';
    this.load();
  }
  load(): void {
    if (!this.clientId) return;
    forkJoin({
      clients: this.api.clients(),
      vehicles: this.api.clientVehicles(this.clientId),
      locations: this.api.clientLocations(this.clientId),
    }).subscribe((x) => {
      const client = x.clients.find((c) => c.id === this.clientId) ?? null;
      this.client.set(client);
      this.profileName = client?.name ?? '';
      this.profilePhone = client?.phone ?? '';
      this.profileNotes = client?.notes ?? '';
      this.vehicles.set(x.vehicles);
      this.locations.set(x.locations);
    });
  }
  setLocation(value: M.LocationInput): void {
    this.locationSelection = { ...value, label: this.label || value.label || 'Local salvo' };
  }
  async saveProfile(): Promise<void> {
    const client = this.client();
    if (!client?.id) return;
    this.busy.set(true);
    try {
      await firstValueFrom(
        this.api.updateClient(client.id, {
          name: this.profileName,
          phone: this.profilePhone,
          notes: this.profileNotes,
        }),
      );
      this.profileDialog.set(false);
      this.messages.success('Perfil atualizado');
      this.load();
    } finally {
      this.busy.set(false);
    }
  }
  async addVehicle(): Promise<void> {
    if (!this.clientId || !this.model || !this.plate) return;
    this.busy.set(true);
    try {
      await firstValueFrom(
        this.api.createClientVehicle(this.clientId, {
          model: this.model,
          manufacturer: this.manufacturer,
          licensePlate: this.plate,
          color: this.color,
          modelYear: this.modelYear,
        }),
      );
      this.vehicleDialog.set(false);
      this.messages.success('Veículo adicionado');
      this.load();
    } finally {
      this.busy.set(false);
    }
  }
  async toggle(v: M.VehicleResponse): Promise<void> {
    if (!this.clientId || !v.id) return;
    await firstValueFrom(this.api.setClientVehicleActive(this.clientId, v.id, !v.isActive));
    this.load();
  }
  async addLocation(): Promise<void> {
    if (!this.clientId || !this.locationSelection) {
      this.messages.warning('Selecione o endereço no mapa.');
      return;
    }
    this.busy.set(true);
    try {
      await firstValueFrom(
        this.api.createClientLocation(this.clientId, {
          ...this.locationSelection,
          label: this.label || this.locationSelection.label,
        }),
      );
      this.locationDialog.set(false);
      this.locationSelection = null;
      this.messages.success('Local salvo');
      this.load();
    } finally {
      this.busy.set(false);
    }
  }
}
