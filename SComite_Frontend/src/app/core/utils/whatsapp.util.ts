export function normalizarTelefonoPeru(telefono: string | null | undefined): string {
  const soloDigitos = (telefono ?? '').replace(/\D/g, '');
  if (!soloDigitos) return '';

  const sinPrefijo51 = soloDigitos.startsWith('51') ? soloDigitos.slice(2) : soloDigitos;
  return `51${sinPrefijo51}`;
}