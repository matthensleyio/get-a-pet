import type { DogDto } from '../types/api';

function effectiveSortDate(dog: DogDto): number {
  if (dog.listingDate) {
    const listing = new Date(dog.listingDate).getTime();
    if (Date.now() - listing < 24 * 60 * 60 * 1000) return listing;
  }
  return new Date(dog.intakeDate ?? dog.listingDate ?? dog.firstSeen).getTime();
}

function parseAgeMonths(age: string | null): number {
  if (!age) return Number.MAX_SAFE_INTEGER;
  const s = age.toLowerCase();
  const yearMatch = s.match(/(\d+)\s*year/);
  const monthMatch = s.match(/(\d+)\s*month/);
  const years = yearMatch ? parseInt(yearMatch[1]) : 0;
  const months = monthMatch ? parseInt(monthMatch[1]) : 0;
  return years === 0 && months === 0 ? Number.MAX_SAFE_INTEGER : years * 12 + months;
}

export function sortAndFilterDogs(
  dogs: DogDto[],
  sort: string,
  shelterIds: string[],
): DogDto[] {
  const filtered =
    shelterIds.length > 0
      ? dogs.filter((d) => shelterIds.includes(d.shelterId))
      : dogs;

  const copy = [...filtered];
  switch (sort) {
    case 'age':
      return copy.sort(
        (a, b) =>
          parseAgeMonths(a.age) - parseAgeMonths(b.age) ||
          (a.name ?? '').localeCompare(b.name ?? ''),
      );
    case 'name':
      return copy.sort((a, b) => (a.name ?? '').localeCompare(b.name ?? ''));
    default:
      return copy.sort(
        (a, b) =>
          effectiveSortDate(b) - effectiveSortDate(a) ||
          (a.name ?? '').localeCompare(b.name ?? ''),
      );
  }
}
