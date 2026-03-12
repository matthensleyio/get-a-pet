export function cleanBreed(breed: string | null): string | null {
  if (!breed) return null;
  return (
    breed
      .replace(/\s*\([^)]+\)/g, '')
      .replace(/\s*\/\s*Mix\s*$/i, '')
      .trim() || null
  );
}
