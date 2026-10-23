import { isCnpj, isCoordinate, isCpf, isPhone, isPlate } from './form-validators';

describe('form validators', () => {
  it('validates Brazilian documents', () => {
    expect(isCpf('529.982.247-25')).toBe(true);
    expect(isCpf('111.111.111-11')).toBe(false);
    expect(isCnpj('04.252.011/0001-10')).toBe(true);
    expect(isCnpj('00.000.000/0000-00')).toBe(false);
  });
  it('validates operational formats', () => {
    expect(isPhone('(11) 99999-9999')).toBe(true);
    expect(isPlate('BRA2E19')).toBe(true);
    expect(isCoordinate(-23.55, -46.63)).toBe(true);
    expect(isCoordinate(120, 0)).toBe(false);
  });
});
