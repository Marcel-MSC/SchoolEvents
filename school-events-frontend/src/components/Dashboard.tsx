import { useState } from 'react';
import { UserList } from './UserList';
import { EventList } from './EventList';
import { useUsers } from '../hooks/useUsers';
import { useUserEvents } from '../hooks/useUserEvents';

export const Dashboard: React.FC = () => {
    const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(10);

    const { 
        data: usersData, 
        isLoading: usersLoading, 
        error: usersError 
    } = useUsers({ page, pageSize });

    const { 
        data: events = [], 
        isLoading: eventsLoading 
    } = useUserEvents(selectedUserId);

    const handleUserSelect = (userId: string) => {
        setSelectedUserId(prev => prev === userId ? null : userId);
    };

    const handlePageChange = (newPage: number) => {
        setPage(newPage);
    };

    const handlePageSizeChange = (newPageSize: number) => {
        setPageSize(newPageSize);
        setPage(1); // Reset para primeira página
    };

    return (
        <div className="max-w-7xl mx-auto py-8 px-4 sm:px-6 lg:px-8">
            <div className="mb-8">
                <h2 className="text-2xl font-bold text-gray-900">Dashboard</h2>
                <p className="text-gray-600 mt-2">
                    Selecione um usuário para visualizar seus eventos
                </p>
            </div>

            {usersError && (
                <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
                    <div className="flex items-center">
                        <svg className="w-5 h-5 text-red-400 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                        <div>
                            <span className="font-medium text-red-800">Erro ao carregar usuários</span>
                            <p className="text-sm text-red-600 mt-1">{usersError.message}</p>
                        </div>
                    </div>
                </div>
            )}

            <div className="bg-white rounded-lg shadow-sm border border-gray-200 flex min-h-[600px]">
                <UserList
                    users={usersData?.items || []}
                    selectedUserId={selectedUserId}
                    onUserSelect={handleUserSelect}
                    isLoading={usersLoading}
                    hasNextPage={usersData?.hasNext || false}
                    hasPreviousPage={usersData?.hasPrevious || false}
                    currentPage={usersData?.currentPage || 1}
                    totalPages={usersData?.totalPages || 1}
                    totalCount={usersData?.totalCount || 0}
                    onPageChange={handlePageChange}
                    onPageSizeChange={handlePageSizeChange}
                />

                <EventList
                    events={events}
                    isLoading={eventsLoading && !!selectedUserId}
                    selectedUserId={selectedUserId}
                />
            </div>

            {selectedUserId && (
                <div className="mt-6 p-4 bg-blue-50 border border-blue-200 rounded-lg">
                    <div className="flex items-center justify-center text-sm text-blue-700">
                        <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                        Mostrando eventos para o usuário selecionado
                    </div>
                </div>
            )}
        </div>
    );
};