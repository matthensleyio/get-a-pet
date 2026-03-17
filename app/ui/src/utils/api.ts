import type { StatusResponseDto, OfflineResponse, PushSubscriptionRequest, ShelterDto } from '../types/api';

export async function fetchStatus(): Promise<StatusResponseDto | OfflineResponse> {
  const res = await fetch('/api/status');
  return res.json() as Promise<StatusResponseDto | OfflineResponse>;
}

export async function fetchVapidKey(): Promise<string> {
  const res = await fetch('/api/vapid-public-key');
  if (!res.ok) throw new Error(`Failed to fetch VAPID key: ${res.status}`);
  const text = await res.text();
  return text.replace(/"/g, '');
}

export async function subscribePush(request: PushSubscriptionRequest): Promise<void> {
  await fetch('/api/subscribe', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });
}

export async function fetchShelters(): Promise<ShelterDto[]> {
  const res = await fetch('/api/shelters');
  if (!res.ok) return [];
  return res.json() as Promise<ShelterDto[]>;
}

export async function unsubscribePush(
  request: Omit<PushSubscriptionRequest, 'shelterIds'>,
): Promise<void> {
  await fetch('/api/subscribe', {
    method: 'DELETE',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });
}
