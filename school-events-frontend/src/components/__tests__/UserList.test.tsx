import { render, screen, fireEvent } from '@testing-library/react';
import { UserList } from '../UserList';
import type { User } from '@/types';

const makeUsers = (count: number): User[] =>
  Array.from({ length: count }).map((_, index) => ({
    id: `user-${index + 1}`,
    microsoftId: `ms-${index + 1}`,
    displayName: `Usuário ${index + 1}`,
    email: `user${index + 1}@teste.com`,
    jobTitle: 'Aluno',
    department: 'Turma A',
    lastSynced: new Date().toISOString(),
  }));

describe('UserList', () => {
  it('exibe a quantidade de usuários encontrada', () => {
    const users = makeUsers(3);

    render(
      <UserList
        users={users}
        selectedUserId={null}
        onUserSelect={() => {}}
        totalCount={users.length}
      />
    );

    expect(screen.getByText('3 usuários encontrados')).toBeInTheDocument();
  });

  it('renderiza o filtro customizado quando fornecido', () => {
    const users = makeUsers(1);

    render(
      <UserList
        users={users}
        selectedUserId={null}
        onUserSelect={() => {}}
        totalCount={users.length}
        filterControl={<span>Filtro customizado</span>}
      />
    );

    expect(screen.getByText('Filtro customizado')).toBeInTheDocument();
  });

  it('dispara onUserSelect ao clicar em um usuário', () => {
    const users = makeUsers(1);
    const handleUserSelect = vi.fn();

    render(
      <UserList
        users={users}
        selectedUserId={null}
        onUserSelect={handleUserSelect}
        totalCount={users.length}
      />
    );

    const card = screen.getByText('Usuário 1');
    fireEvent.click(card);

    expect(handleUserSelect).toHaveBeenCalledWith('user-1');
  });
});
