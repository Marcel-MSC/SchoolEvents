import type { CalendarEvent } from '@/types';

interface EventListProps {
  events: CalendarEvent[];
  isLoading?: boolean;
  selectedUserId?: string | null;
}

export const EventList: React.FC<EventListProps> = ({ 
  events, 
  isLoading = false,
  selectedUserId 
}) => {
  if (!selectedUserId) {
    return (
      <div className="flex-1 p-6 flex items-center justify-center">
        <div className="text-center text-gray-500">
          <svg className="w-16 h-16 mx-auto mb-4 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
          <p>Selecione um usuário para visualizar os eventos</p>
        </div>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="flex-1 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Eventos</h3>
        <div className="space-y-3">
          {[1, 2, 3, 4, 5].map(i => (
            <div key={i} className="p-4 border border-gray-200 rounded-lg animate-pulse">
              <div className="h-4 bg-gray-200 rounded w-3/4 mb-2"></div>
              <div className="h-3 bg-gray-200 rounded w-1/2 mb-2"></div>
              <div className="h-3 bg-gray-200 rounded w-1/3"></div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="flex-1 p-6">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">
        Eventos ({events.length})
      </h3>
      
      {events.length === 0 ? (
        <div className="text-center py-8 text-gray-500">
          <svg className="w-12 h-12 mx-auto mb-3 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
          <p>Nenhum evento encontrado</p>
        </div>
      ) : (
        <div className="space-y-3">
          {events.map(event => (
            <div key={event.id} className="p-4 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors">
              <h4 className="font-medium text-gray-900 mb-2">{event.subject}</h4>
              <div className="text-sm text-gray-600 space-y-1">
                <div className="flex items-center">
                  <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  {new Date(event.startTime).toLocaleString('pt-BR')} - {new Date(event.endTime).toLocaleString('pt-BR')}
                </div>
                {event.location && (
                  <div className="flex items-center">
                    <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                    </svg>
                    {event.location}
                  </div>
                )}
                {event.isAllDay && (
                  <div className="inline-flex items-center px-2 py-1 bg-blue-100 text-blue-800 text-xs rounded-full">
                    Dia inteiro
                  </div>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};