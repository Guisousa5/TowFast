export const digits = (value: string): string => value.replace(/\D/g, '');

export function isCpf(value: string): boolean {
  const cpf = digits(value);
  if (cpf.length !== 11 || /^(\d)\1+$/.test(cpf)) return false;
  const check = (length: number): number => {
    let sum = 0;
    for (let i = 0; i < length; i++) sum += Number(cpf[i]) * (length + 1 - i);
    const result = (sum * 10) % 11;
    return result === 10 ? 0 : result;
  };
  return check(9) === Number(cpf[9]) && check(10) === Number(cpf[10]);
}

export function isCnpj(value: string): boolean {
  const cnpj = digits(value);
  if (cnpj.length !== 14 || /^(\d)\1+$/.test(cnpj)) return false;
  const calculate = (length: number): number => {
    const weights = length === 12 ? [5,4,3,2,9,8,7,6,5,4,3,2] : [6,5,4,3,2,9,8,7,6,5,4,3,2];
    const rest = weights.reduce((sum, weight, index) => sum + Number(cnpj[index]) * weight, 0) % 11;
    return rest < 2 ? 0 : 11 - rest;
  };
  return calculate(12) === Number(cnpj[12]) && calculate(13) === Number(cnpj[13]);
}

export const isPhone = (value: string): boolean => [10, 11].includes(digits(value).length);
export const isPlate = (value: string): boolean => /^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$/.test(value.replace(/[^a-z0-9]/gi, '').toUpperCase());
export const isCoordinate = (latitude: number | undefined, longitude: number | undefined): boolean => latitude != null && longitude != null && latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
