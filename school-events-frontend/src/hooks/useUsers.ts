import { useQuery } from '@tanstack/react-query';
import api from '@/services/api';
import type { User, PagedResult } from '@/types';

interface UseUsersProps {
  page?: number;
  pageSize?: number;
  enabled?: boolean;
  onlyWithEvents?: boolean
}

export const useUsers = ({ 
  page = 1, 
  pageSize = 10, 
  enabled= true,
  onlyWithEvents = false
}: UseUsersProps = {}) => {
  return useQuery({
    queryKey: ['users', page, pageSize, !!onlyWithEvents],
    queryFn: async (): Promise<PagedResult<User>> => {
      const response = await api.get<PagedResult<User>>('/users', {
        params: { 
          page, 
          pageSize,
          onlyWithEvents,
        },
      });
      
      return response.data;
    },
    enabled,
    // keepPreviousData: true,
    staleTime: 2 * 60 * 1000, // 2 minutos
  });
};