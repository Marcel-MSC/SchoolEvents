import React from 'react';
import { useAuth } from '../contexts/AuthContext';

export const Header: React.FC = () => {
    const { user, logout } = useAuth();

    return (
        <header className="bg-white shadow-sm border-b border-gray-200">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                <div className="flex justify-between items-center py-4">
                    <div>
                        <h1 className="text-2xl font-bold text-gray-900">School Events</h1>
                        <p className="text-sm text-gray-600">Sistema de gerenciamento de eventos</p>
                    </div>

                    <div className="flex items-center space-x-4">
                        <div className="text-right">
                            <p className="text-sm font-medium text-gray-900">{user?.displayName}</p>
                            <p className="text-sm text-gray-600">{user?.email}</p>
                        </div>
                        <button
                            onClick={logout}
                            className="bg-gray-200 hover:bg-gray-300 text-gray-800 px-4 py-2 rounded-md text-sm font-medium transition-colors"
                        >
                            Sair
                        </button>
                    </div>
                </div>
            </div>
        </header>
    );
};