import type {
  StatusResponseDto,
  OfflineResponse,
  PushSubscriptionRequest,
  DogDto,
  AdoptedDogDto,
} from '../types/api';

export async function fetchStatus(): Promise<StatusResponseDto | OfflineResponse> {
  const res = await fetch('/api/status');
  return res.json() as Promise<StatusResponseDto | OfflineResponse>;
}

export async function fetchDog(
  aid: string,
): Promise<DogDto | AdoptedDogDto | OfflineResponse | null> {
  const res = await fetch(`/api/dogs/${aid}`);
  if (res.status === 404) return null;
  return res.json() as Promise<DogDto | AdoptedDogDto | OfflineResponse>;
}

export interface MonitorResult {
  offline?: boolean;
}

export async function triggerMonitor(force = false): Promise<MonitorResult> {
  const url = force ? '/api/monitor?force=true' : '/api/monitor';
  const res = await fetch(url, { method: 'POST' });
  if (res.ok) return res.json() as Promise<MonitorResult>;
  if (res.status === 500) {
    const problem = await res.json().catch(() => ({})) as Record<string, string>;
    throw new Error(problem['detail'] ?? problem['title'] ?? 'Monitor check failed');
  }
  return {};
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

export async function unsubscribePush(
  request: Omit<PushSubscriptionRequest, 'shelterIds'>,
): Promise<void> {
  await fetch('/api/subscribe', {
    method: 'DELETE',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });
}
