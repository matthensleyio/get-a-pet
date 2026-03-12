import { differenceInDays, differenceInHours, differenceInMinutes, differenceInSeconds } from 'date-fns';

export function timeAgo(dateStr: string): string {
  const date = new Date(dateStr);
  const now = new Date();
  const seconds = differenceInSeconds(now, date);
  if (seconds < 60) return 'just now';
  const minutes = differenceInMinutes(now, date);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = differenceInHours(now, date);
  if (hours < 24) return `${hours}h ago`;
  const days = differenceInDays(now, date);
  return `${days}d ago`;
}
