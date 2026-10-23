import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { firstValueFrom, forkJoin } from 'rxjs';
import { TowFastApiService } from '../../core/towfast-api.service';
import { SessionService } from '../../core/session.service';
import * as M from '../../api/generated/models';
@Component({
  selector: 'app-team-page',
  imports: [
    FormsModule,
    DatePipe,
    NzButtonModule,
    NzCheckboxModule,
    NzIconModule,
    NzInputModule,
    NzInputNumberModule,
    NzModalModule,
  ],
  templateUrl: './team-page.html',
  styleUrl: './team-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeamPage implements OnInit {
  private readonly api = inject(TowFastApiService);
  private readonly session = inject(SessionService);
  private readonly route = inject(ActivatedRoute);
  private readonly messages = inject(NzMessageService);
  private providerId = '';
  readonly drivers = signal<M.DriverResponse[]>([]);
  readonly vehicles = signal<M.ProviderVehicleResponse[]>([]);
  readonly types = signal<M.ServiceTypeResponse[]>([]);
  readonly selectedTypes = signal<string[]>([]);
  readonly driverDialog = signal(false);
  readonly vehicleDialog = signal(false);
  readonly busy = signal(false);
  fullName = '';
  document = '';
  phone = '';
  email = '';
  password = '';
  licenseNumber = '';
  licenseCategory = 'B';
  licenseExpiresOn = '';
  plate = '';
  vehicleType = 'Reboque';
  description = '';
  capacityKg?: number;
  ngOnInit() {
    const canSelectScope = ['Admin', 'Operator'].includes(this.session.role() ?? '');
    this.providerId =
      (canSelectScope ? this.route.snapshot.queryParamMap.get('providerId') : null) ??
      this.session.user()?.providerId ??
      '';
    this.load();
  }
  load() {
    if (!this.providerId) return;
    forkJoin({
      drivers: this.api.providerDrivers(this.providerId),
      vehicles: this.api.providerVehicles(this.providerId),
      types: this.api.serviceTypes(),
      selected: this.api.providerServiceTypes(this.providerId),
    }).subscribe((x) => {
      this.drivers.set(x.drivers);
      this.vehicles.set(x.vehicles);
      this.types.set(x.types);
      this.selectedTypes.set(x.selected.map((t) => t.id!).filter(Boolean));
    });
  }
  async addDriver() {
    if (!this.providerId || !this.fullName || !this.email) return;
    this.busy.set(true);
    try {
      await firstValueFrom(
        this.api.createDriver(this.providerId, {
          fullName: this.fullName,
          document: this.document,
          phone: this.phone,
          email: this.email,
          password: this.password,
          licenseNumber: this.licenseNumber,
          licenseCategory: this.licenseCategory,
          licenseExpiresOn: this.licenseExpiresOn,
        }),
      );
      this.driverDialog.set(false);
      this.messages.success('Motorista criado');
      this.load();
    } finally {
      this.busy.set(false);
    }
  }
  async toggleDriver(d: M.DriverResponse) {
    if (!this.providerId || !d.id) return;
    await firstValueFrom(
      this.api.updateDriver(this.providerId, d.id, { ...d, isActive: !d.isActive }),
    );
    this.load();
  }
  async addVehicle() {
    if (!this.providerId || !this.plate) return;
    this.busy.set(true);
    try {
      await firstValueFrom(
        this.api.createProviderVehicle(this.providerId, {
          licensePlate: this.plate,
          vehicleType: this.vehicleType,
          description: this.description,
          capacityKg: this.capacityKg,
        }),
      );
      this.vehicleDialog.set(false);
      this.messages.success('Veículo incluído na frota');
      this.load();
    } finally {
      this.busy.set(false);
    }
  }
  async toggleVehicle(v: M.ProviderVehicleResponse) {
    if (!this.providerId || !v.id) return;
    await firstValueFrom(
      this.api.updateProviderVehicle(this.providerId, v.id, {
        vehicleType: v.vehicleType,
        description: v.description,
        capacityKg: v.capacityKg,
        isActive: !v.isActive,
      }),
    );
    this.load();
  }
  toggleType(id: string) {
    this.selectedTypes.update((x) => (x.includes(id) ? x.filter((i) => i !== id) : [...x, id]));
  }
  async saveTypes() {
    if (!this.providerId) return;
    await firstValueFrom(this.api.setProviderServiceTypes(this.providerId, this.selectedTypes()));
    this.messages.success('Serviços atualizados');
  }
}
