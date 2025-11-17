import { useState } from 'react';
import type { User } from '@/types';

interface UserListProps {
  users: User[];
  selectedUserId: string | null;
  onUserSelect: (userId: string) => void;
  isLoading?: boolean;
  hasNextPage?: boolean;
  hasPreviousPage?: boolean;
  currentPage?: number;
  totalPages?: number;
  totalCount?: number;
  onPageChange?: (page: number) => void;
  onPageSizeChange?: (pageSize: number) => void;
  filterControl?: React.ReactNode;
}

export const UserList: React.FC<UserListProps> = ({ 
  users, 
  selectedUserId, 
  onUserSelect,
  isLoading = false,
  hasNextPage = false,
  hasPreviousPage = false,
  currentPage = 1,
  totalPages = 1,
  totalCount = 0,
  onPageChange,
  onPageSizeChange,
  filterControl,
}) => {
  const [pageSize, setPageSize] = useState(10);

  const handlePageSizeChange = (newPageSize: number) => {
    setPageSize(newPageSize);
    onPageSizeChange?.(newPageSize);
  };

  const handlePreviousPage = () => {
    if (hasPreviousPage && currentPage > 1) {
      onPageChange?.(currentPage - 1);
    }
  };

  const handleNextPage = () => {
    if (hasNextPage && currentPage < totalPages) {
      onPageChange?.(currentPage + 1);
    }
  };

  // Skeleton loading para estado inicial
  if (isLoading && users.length === 0) {
    return (
      <div className="w-1/3 border-r border-gray-200 p-6">
        <div className="flex justify-between items-center mb-6">
          <h3 className="text-lg font-semibold text-gray-900">Usuários</h3>
          <div className="h-8 bg-gray-200 rounded w-24 animate-pulse"></div>
        </div>
        <div className="space-y-3">
          {[1, 2, 3, 4, 5, 6].map(i => (
            <div key={i} className="p-4 border border-gray-200 rounded-lg animate-pulse">
              <div className="h-4 bg-gray-200 rounded w-3/4 mb-2"></div>
              <div className="h-3 bg-gray-200 rounded w-1/2"></div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="w-1/3 border-r border-gray-200 p-6 flex flex-col">
      {/* Header */}
      <div className="flex justify-between items-center mb-6">
        <div>
          <h3 className="text-lg font-semibold text-gray-900">Usuários</h3>
          <p className="text-sm text-gray-500 mt-1">
            {totalCount > 0 ? `${totalCount} usuários encontrados` : 'Nenhum usuário'}
          </p>
        </div>
        {filterControl && (
          <div className="ml-4">
            {filterControl}
          </div>
        )}
      </div>

      {/* Pagination Controls - Apenas se tiver paginação */}
      {(onPageChange || onPageSizeChange) && totalCount > 0 && (
        <div className="flex items-center justify-between mb-4 p-3 bg-gray-50 rounded-lg">
          {/* Page Size Selector */}
          {onPageSizeChange && (
            <div className="flex items-center space-x-2">
              <label htmlFor="pageSize" className="text-sm text-gray-600 whitespace-nowrap">
                Mostrar:
              </label>
              <select 
                id="pageSize"
                value={pageSize}
                onChange={(e) => handlePageSizeChange(Number(e.target.value))}
                className="text-sm border border-gray-300 rounded-md px-2 py-1 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                disabled={isLoading}
              >
                <option value={10}>10</option>
                <option value={50}>50</option>
                <option value={100}>100</option>
              </select>
            </div>
          )}

          {/* Pagination Info */}
          <div className="text-sm text-gray-600 whitespace-nowrap">
            Página <span className="font-medium">{currentPage}</span> de <span className="font-medium">{totalPages}</span>
          </div>

          {/* Pagination Buttons */}
          {onPageChange && (
            <div className="flex space-x-1">
              <button
                onClick={handlePreviousPage}
                disabled={!hasPreviousPage || isLoading}
                className="p-2 rounded-md disabled:opacity-50 disabled:cursor-not-allowed hover:bg-white transition-colors border border-gray-300"
                title="Página anterior"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
                </svg>
              </button>
              <button
                onClick={handleNextPage}
                disabled={!hasNextPage || isLoading}
                className="p-2 rounded-md disabled:opacity-50 disabled:cursor-not-allowed hover:bg-white transition-colors border border-gray-300"
                title="Próxima página"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                </svg>
              </button>
            </div>
          )}
        </div>
      )}

      {/* Users List */}
      <div className="flex-1 overflow-y-auto space-y-3">
        {users.length === 0 ? (
          <div className="text-center py-8 text-gray-500">
            <svg className="w-12 h-12 mx-auto mb-3 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197m13.5-9a2.5 2.5 0 11-5 0 2.5 2.5 0 015 0z" />
            </svg>
            <p className="text-sm">Nenhum usuário encontrado</p>
            <p className="text-xs text-gray-400 mt-1">Tente ajustar os filtros ou sincronizar os dados</p>
          </div>
        ) : (
          users.map(user => (
            <div
              key={user.id}
              className={`p-4 border rounded-lg cursor-pointer transition-all duration-200 ${
                selectedUserId === user.id 
                  ? 'bg-blue-50 border-blue-500 shadow-sm ring-1 ring-blue-500' 
                  : 'border-gray-200 hover:bg-gray-50 hover:border-gray-300'
              } ${isLoading ? 'opacity-60' : ''}`}
              onClick={() => !isLoading && onUserSelect(user.id)}
            >
              <div className="flex justify-between items-start">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center space-x-2 mb-1">
                    <p className="font-semibold text-gray-900 truncate" title={user.displayName}>
                      {user.displayName}
                    </p>
                    {selectedUserId === user.id && (
                      <div className="flex-shrink-0">
                        <div className="w-2 h-2 bg-blue-500 rounded-full"></div>
                      </div>
                    )}
                  </div>
                  <p className="text-sm text-gray-600 truncate mb-1" title={user.email}>
                    {user.email}
                  </p>
                  {user.department && (
                    <p className="text-xs text-gray-500 truncate" title={user.department}>
                      {user.department}
                    </p>
                  )}
                  {user.jobTitle && (
                    <p className="text-xs text-gray-400 mt-1 truncate" title={user.jobTitle}>
                      {user.jobTitle}
                    </p>
                  )}
                </div>
              </div>
              <div className="text-xs text-gray-400 mt-2">
                Atualizado: {new Date(user.lastSynced).toLocaleDateString('pt-BR')}
              </div>
            </div>
          ))
        )}
      </div>

      {/* Loading indicator for pagination */}
      {isLoading && users.length > 0 && (
        <div className="mt-4 flex justify-center">
          <div className="inline-flex items-center px-4 py-2 text-sm bg-blue-50 text-blue-700 rounded-lg border border-blue-200">
            <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-blue-700" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            Carregando...
          </div>
        </div>
      )}
    </div>
  );
};