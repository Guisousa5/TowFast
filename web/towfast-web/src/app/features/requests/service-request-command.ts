import {
  CreateServiceRequestCommand,
  LocationInput,
  ServiceTypeResponse,
} from '../../api/generated/models';

export interface CreateRequestFormValue {
  serviceType: ServiceTypeResponse;
  clientVehicleId: string;
  originLocationId?: string;
  origin?: LocationInput | null;
  destinationLocationId?: string;
  destination?: LocationInput | null;
  customerNotes?: string;
}

export function buildCreateRequestCommand(
  value: CreateRequestFormValue,
): CreateServiceRequestCommand {
  const requiresDestination =
    value.serviceType.code === 'Tow' || value.serviceType.requiresDestination === true;
  if (!value.serviceType.id || !value.clientVehicleId)
    throw new Error('Serviço e veículo são obrigatórios.');
  if (!value.originLocationId && !value.origin) throw new Error('A origem é obrigatória.');
  if (requiresDestination && !value.destinationLocationId && !value.destination)
    throw new Error('O destino é obrigatório para reboque.');

  return {
    serviceTypeId: value.serviceType.id,
    clientVehicleId: value.clientVehicleId,
    customerNotes: value.customerNotes,
    originLocationId: value.originLocationId || null,
    origin: value.originLocationId ? undefined : (value.origin ?? undefined),
    destinationLocationId: requiresDestination ? value.destinationLocationId || null : null,
    destination:
      requiresDestination && !value.destinationLocationId
        ? (value.destination ?? undefined)
        : undefined,
  };
}
