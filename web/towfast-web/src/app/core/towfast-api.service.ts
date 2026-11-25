import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { RuntimeConfigService } from './runtime-config.service';
import * as M from '../api/generated/models';
import { SILENT_HTTP_ERROR } from './http-context-tokens';

type Query = Record<string, string | number | boolean | null | undefined>;

@Injectable({ providedIn: 'root' })
export class TowFastApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(RuntimeConfigService);
  private url(path: string): string {
    return `${this.config.apiBaseUrl}${path}`;
  }
  private params(query: Query): HttpParams {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query))
      if (value !== null && value !== undefined && value !== '') params = params.set(key, value);
    return params;
  }

  register(body: M.RegisterClientCommand) {
    return this.http.post<M.AuthResponse>(this.url('/api/v1/auth/register'), body);
  }
  login(body: M.LoginCommand) {
    return this.http.post<M.AuthResponse>(this.url('/api/v1/auth/login'), body);
  }
  refresh(refreshToken: string) {
    return this.http.post<M.AuthResponse>(this.url('/api/v1/auth/refresh'), { refreshToken });
  }
  logout(refreshToken: string | null) {
    return this.http.post<void>(this.url('/api/v1/auth/logout'), { refreshToken });
  }

  clients() {
    return this.http.get<M.ClientResponse[]>(this.url('/api/v1/clients'));
  }
  createClient(body: M.ClientCreateCommand) {
    return this.http.post<M.ClientResponse>(this.url('/api/v1/clients'), body);
  }
  updateClient(id: string, body: M.ClientUpdateCommand) {
    return this.http.patch<M.ClientResponse>(this.url(`/api/v1/clients/${id}`), body);
  }
  clientVehicles(clientId: string) {
    return this.http.get<M.VehicleResponse[]>(this.url(`/api/v1/clients/${clientId}/vehicles`));
  }
  createClientVehicle(clientId: string, body: M.VehicleCreateCommand) {
    return this.http.post<M.VehicleResponse>(
      this.url(`/api/v1/clients/${clientId}/vehicles`),
      body,
    );
  }
  setClientVehicleActive(clientId: string, vehicleId: string, isActive: boolean) {
    return this.http.patch<M.VehicleResponse>(
      this.url(`/api/v1/clients/${clientId}/vehicles/${vehicleId}/active`),
      { isActive },
    );
  }
  clientLocations(clientId: string) {
    return this.http.get<M.LocationResponse[]>(this.url(`/api/v1/clients/${clientId}/locations`));
  }
  createClientLocation(clientId: string, body: M.LocationInput) {
    return this.http.post<M.LocationResponse>(
      this.url(`/api/v1/clients/${clientId}/locations`),
      body,
    );
  }

  providers() {
    return this.http.get<M.ProviderResponse[]>(this.url('/api/v1/providers'));
  }
  createProvider(body: M.ProviderCreateCommand) {
    return this.http.post<M.ProviderResponse>(this.url('/api/v1/providers'), body);
  }
  updateProvider(id: string, body: M.ProviderUpdateCommand) {
    return this.http.patch<M.ProviderResponse>(this.url(`/api/v1/providers/${id}`), body);
  }
  setProviderActive(id: string, isActive: boolean) {
    return this.http.patch<M.ProviderResponse>(this.url(`/api/v1/providers/${id}/active`), {
      isActive,
    });
  }
  providerDrivers(providerId: string) {
    return this.http.get<M.DriverResponse[]>(this.url(`/api/v1/providers/${providerId}/drivers`));
  }
  createDriver(providerId: string, body: M.DriverCreateCommand) {
    return this.http.post<M.DriverResponse>(
      this.url(`/api/v1/providers/${providerId}/drivers`),
      body,
    );
  }
  updateDriver(providerId: string, driverId: string, body: M.DriverUpdateCommand) {
    return this.http.patch<M.DriverResponse>(
      this.url(`/api/v1/providers/${providerId}/drivers/${driverId}`),
      body,
    );
  }
  providerVehicles(providerId: string) {
    return this.http.get<M.ProviderVehicleResponse[]>(
      this.url(`/api/v1/providers/${providerId}/vehicles`),
    );
  }
  createProviderVehicle(providerId: string, body: M.ProviderVehicleCreateCommand) {
    return this.http.post<M.ProviderVehicleResponse>(
      this.url(`/api/v1/providers/${providerId}/vehicles`),
      body,
    );
  }
  updateProviderVehicle(
    providerId: string,
    vehicleId: string,
    body: M.ProviderVehicleUpdateCommand,
  ) {
    return this.http.patch<M.ProviderVehicleResponse>(
      this.url(`/api/v1/providers/${providerId}/vehicles/${vehicleId}`),
      body,
    );
  }
  providerServiceTypes(providerId: string) {
    return this.http.get<M.ServiceTypeResponse[]>(
      this.url(`/api/v1/providers/${providerId}/service-types`),
    );
  }
  setProviderServiceTypes(providerId: string, serviceTypeIds: string[]) {
    return this.http.put<void>(this.url(`/api/v1/providers/${providerId}/service-types`), {
      serviceTypeIds,
    });
  }

  serviceTypes() {
    return this.http.get<M.ServiceTypeResponse[]>(this.url('/api/v1/service-types'));
  }
  setServiceTypeActive(id: string, isActive: boolean) {
    return this.http.patch<M.ServiceTypeResponse>(this.url(`/api/v1/service-types/${id}/active`), {
      isActive,
    });
  }
  configureServicePrice(id: string, body: M.ConfigureServicePriceCommand) {
    return this.http.patch<M.ServiceTypeResponse>(
      this.url(`/api/v1/service-types/${id}/pricing`), body,
    );
  }
  requests(query: Query = {}) {
    return this.http.get<M.ServiceRequestResponsePageResult>(this.url('/api/v1/service-requests'), {
      params: this.params(query),
    });
  }
  availableRequests(query: Query = {}) {
    return this.http.get<M.AvailableRequestResponsePageResult>(
      this.url('/api/v1/service-requests/available'),
      { params: this.params(query) },
    );
  }
  request(publicId: string) {
    return this.http.get<M.ServiceRequestDetailResponse>(
      this.url(`/api/v1/service-requests/${publicId}`),
    );
  }
  createRequest(body: M.CreateServiceRequestCommand) {
    return this.http.post<M.ServiceRequestResponse>(this.url('/api/v1/service-requests'), body);
  }
  quoteRequest(body: M.CreateServiceRequestCommand) {
    return this.http.post<M.PriceQuoteResponse>(this.url('/api/v1/service-requests/quote'), body);
  }
  acceptRequest(id: string, body: M.AcceptServiceRequestCommand) {
    return this.http.post<M.ServiceRequestResponse>(
      this.url(`/api/v1/service-requests/${id}/accept`),
      body,
    );
  }
  releaseRequest(id: string, version?: number, reason?: string) {
    return this.http.post<M.ServiceRequestResponse>(
      this.url(`/api/v1/service-requests/${id}/release`),
      { version, reason },
    );
  }
  enRouteRequest(id: string, version?: number) {
    return this.http.post<M.ServiceRequestResponse>(
      this.url(`/api/v1/service-requests/${id}/en-route`),
      { version },
    );
  }
  startRequest(id: string, version?: number) {
    return this.http.post<M.ServiceRequestResponse>(
      this.url(`/api/v1/service-requests/${id}/start`),
      { version },
    );
  }
  completeRequest(id: string, body: M.CompleteServiceRequestCommand) {
    return this.http.post<M.ServiceRequestResponse>(
      this.url(`/api/v1/service-requests/${id}/complete`),
      body,
    );
  }
  cancelRequest(id: string, version?: number, reason?: string) {
    return this.http.post<M.ServiceRequestResponse>(
      this.url(`/api/v1/service-requests/${id}/cancel`),
      { version, reason },
    );
  }
  publishPosition(requestId: string, body: M.PublishPositionCommand) {
    return this.http.post<M.TrackingResponse>(
      this.url(`/api/v1/service-requests/${requestId}/tracking/positions`),
      body,
      { context: new HttpContext().set(SILENT_HTTP_ERROR, true) },
    );
  }
  health() {
    return this.http.get(this.url('/health'), { responseType: 'text' });
  }
  healthLive() {
    return this.http.get(this.url('/health/live'), { responseType: 'text' });
  }
  latestPosition(requestId: string) {
    return this.http.get<M.TrackingResponse>(
      this.url(`/api/v1/service-requests/${requestId}/tracking/latest`),
      { context: new HttpContext().set(SILENT_HTTP_ERROR, true) },
    );
  }
}
