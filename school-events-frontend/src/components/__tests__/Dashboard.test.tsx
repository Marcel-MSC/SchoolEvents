import { render, screen, fireEvent } from '@testing-library/react'; // eslint-disable-line import/no-unresolved
import { Dashboard } from '../Dashboard';

vi.mock('../../hooks/useUsers', () => ({
  useUsers: () => ({
    data: {
      items: [
        {
          id: 'user-1',
          microsoftId: 'ms-1',
          displayName: 'Usuário 1',
          email: 'user1@teste.com',
          jobTitle: 'Aluno',
          department: 'Turma A',
          lastSynced: new Date().toISOString(),
        },
      ],
      totalCount: 1,
      currentPage: 1,
      totalPages: 1,
      hasNext: false,
      hasPrevious: false,
    },
    isLoading: false,
    error: null,
  }),
}));

vi.mock('../../hooks/useUserEvents', () => ({
  useUserEvents: () => ({
    data: [],
    isLoading: false,
    error: null,
  }),
}));

describe('Dashboard', () => {
  it('renderiza título e lista de usuários', () => {
    render(<Dashboard />);

    expect(screen.getByText('Dashboard')).toBeInTheDocument();
    expect(screen.getByText('Usuários')).toBeInTheDocument();
    expect(screen.getByText('1 usuários encontrados')).toBeInTheDocument();
  });

  it('permite alternar o checkbox "Somente com eventos"', () => {
    render(<Dashboard />);

    const checkbox = screen.getByLabelText('Somente com eventos') as HTMLInputElement;
    expect(checkbox.checked).toBe(false);

    fireEvent.click(checkbox);
    expect(checkbox.checked).toBe(true);

    fireEvent.click(checkbox);
    expect(checkbox.checked).toBe(false);
  });
});
