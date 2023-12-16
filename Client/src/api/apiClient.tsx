import axios, { InternalAxiosRequestConfig } from 'axios';

const authRequestInterceptor = (config: InternalAxiosRequestConfig) => {
    const token = sessionStorage.getItem('jwtToken');
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
};

export const ApiClient = axios.create({
    baseURL: import.meta.env.VITE_ROOT_URL,
    withCredentials: true,
});

ApiClient.interceptors.request.use(authRequestInterceptor);

ApiClient.interceptors.response.use(
    (response) => {
        return response.data;
    },
    async (error) => {
        const originalRequest = error.config;

        if (error.response.status === 401) {
            // Very important to return a promise, otherwise react-query get error before this interceptor finished
            return new Promise((resolve, reject) => {
                axios({
                    method: 'GET',
                    url: `${import.meta.env.VITE_ROOT_URL}api/Accounts/renew-tokens`,
                    withCredentials: true,
                    headers: {
                        Accept: 'application/json',
                    },
                })
                    .then((response) => {
                        sessionStorage.setItem('jwtToken', response.data.accessToken);
                        originalRequest.headers.Authorization = `Bearer ${response.data.accessToken}`;

                        ApiClient(originalRequest)
                            .then((response) => {
                                resolve(response);
                            })
                            .catch((error) => {
                                reject(error.response?.data);
                            });
                    })
                    .catch((error) => {
                        reject(error.response?.data);
                    });
            });
        } else {
            return Promise.reject(error.response?.data);
        }
    }
);
