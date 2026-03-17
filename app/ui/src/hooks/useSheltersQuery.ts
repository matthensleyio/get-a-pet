import { useQuery } from '@tanstack/react-query';
import { fetchShelters } from '../utils/api';
import type { ShelterDto } from '../types/api';

export function useSheltersQuery(): ShelterDto[] {
  const { data } = useQuery<ShelterDto[]>({
    queryKey: ['shelters'] as const,
    queryFn: fetchShelters,
    staleTime: 5 * 60 * 1000,
    retry: false,
  });
  return data ?? [];
}
