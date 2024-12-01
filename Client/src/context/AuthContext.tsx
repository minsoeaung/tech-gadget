import { createContext, ReactNode, useContext } from 'react';
import { useLocalStorage } from '../hooks/useLocalStorage.ts';
import { ApiClient } from '../api/apiClient.tsx';
import { RegisterDto } from '../types/registerDto.ts';
import { AuthResponse, User } from '../types/authResponse.ts';
import { useQueryClient } from 'react-query';
import { useNavigate } from 'react-router-dom';

interface IAuthContext {
    user: User | null;
    setUser: (user: User | null) => void;
    login: (username: string, password: string) => Promise<void>;
    logout: () => Promise<void>;
    register: (body: RegisterDto) => Promise<void>;
}

const AuthContext = createContext<IAuthContext>({
    user: null,
    setUser: () => {},
    login: async () => {},
    logout: async () => {},
    register: async () => {},
});

export const AuthContextProvider = ({ children }: { children: ReactNode }) => {
    const [user, setUser] = useLocalStorage<User | null>('currentUser', null);

    const queryClient = useQueryClient();

    const navigate = useNavigate();

    const login = async (userName: string, password: string) => {
        const data = await ApiClient.post<never, AuthResponse>('api/Accounts/login', { userName, password });
        sessionStorage.setItem('jwtToken', data.accessToken);
        setUser(data.accountDetails);
    };

    const logout = async () => {
        sessionStorage.removeItem('jwtToken');
        queryClient.clear();
        setUser(null);
        if (window.location.pathname.includes('user/')) {
            navigate('/');
        }
        await ApiClient.get('api/Accounts/logout');
    };

    const register = async (body: RegisterDto) => {
        const data = await ApiClient.post<never, AuthResponse>('api/Accounts/register', body);
        sessionStorage.setItem('jwtToken', data.accessToken);
        setUser(data.accountDetails);
    };

    return <AuthContext.Provider value={{ user, setUser, logout, login, register }}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
    return useContext(AuthContext);
};
