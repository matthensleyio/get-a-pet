export interface PushKeysDto {
  p256dh: string;
  auth: string;
}

export interface PushSubscriptionRequest {
  endpoint: string;
  keys: PushKeysDto;
  shelterIds?: string[];
}

export interface DogDto {
  aid: string;
  shelterId: string;
  name: string | null;
  age: string | null;
  gender: string | null;
  photoUrl: string | null;
  breed: string | null;
  color: string | null;
  size: string | null;
  weight: string | null;
  adoptionFee: string | null;
  currentLocation: string | null;
  profileUrl: string | null;
  firstSeen: string;
  intakeDate: string | null;
  listingDate: string | null;
}

export interface AdoptedDogDto {
  aid: string;
  shelterId: string;
  name: string | null;
  age: string | null;
  gender: string | null;
  photoUrl: string | null;
  breed: string | null;
  color: string | null;
  size: string | null;
  weight: string | null;
  adoptionFee: string | null;
  currentLocation: string | null;
  profileUrl: string | null;
  firstSeen: string;
  intakeDate: string | null;
  adoptedAt: string;
}

export interface StatusResponseDto {
  dogs: DogDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  lastChecked: string | null;
  isMonitoringActive: boolean;
  recentlyAdopted: AdoptedDogDto[];
}

export interface OfflineResponse {
  offline: true;
}

export interface CachedStatusData extends StatusResponseDto {
  fromCache: boolean;
}
