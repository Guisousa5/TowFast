import { describe, expect, it } from 'vitest';
import { buildCreateRequestCommand } from './service-request-command';

const tow = {
  id: 'service-tow',
  code: 'Tow' as const,
  name: 'Reboque',
  requiresDestination: true,
  isActive: true,
};

describe('buildCreateRequestCommand', () => {
  it('sends an inline destination for a tow request', () => {
    const destination = {
      label: 'Destino',
      street: 'Rua B',
      city: 'São Paulo',
      state: 'SP',
      latitude: -23.5,
      longitude: -46.6,
    };
    const command = buildCreateRequestCommand({
      serviceType: tow,
      clientVehicleId: 'vehicle',
      originLocationId: 'origin',
      destination,
    });
    expect(command.destination).toEqual(destination);
    expect(command.destinationLocationId).toBeNull();
  });

  it('sends a saved destination id for a tow request', () => {
    const command = buildCreateRequestCommand({
      serviceType: tow,
      clientVehicleId: 'vehicle',
      originLocationId: 'origin',
      destinationLocationId: 'destination',
    });
    expect(command.destinationLocationId).toBe('destination');
    expect(command.destination).toBeUndefined();
  });

  it('rejects a tow request without a destination', () => {
    expect(() =>
      buildCreateRequestCommand({
        serviceType: tow,
        clientVehicleId: 'vehicle',
        originLocationId: 'origin',
      }),
    ).toThrow('destino');
  });
});
