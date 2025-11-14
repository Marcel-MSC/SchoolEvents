import { useQuery } from '@tanstack/react-query';
import api from '@/services/api';
import type { CalendarEvent } from '@/types';

export const useUserEvents = (userId: string | null) => {
  return useQuery({
    queryKey: ['events', userId],
    queryFn: async (): Promise<CalendarEvent[]> => {
      if (!userId) return [];
      const response = await api.get<CalendarEvent[]>(`/users/${userId}/events`);
      return response.data;
    },
    enabled: !!userId,
  });
};