import { createBrowserRouter, Navigate, Outlet } from 'react-router-dom';
import Root from './Root';
import { lazy } from 'react';
import NotFoundPage from './NotFound';
import { useAuth } from '../context/AuthContext.tsx';
import CatalogPage from './Catalog';

const About = lazy(() => import('./About'));
const Register = lazy(() => import('./User/Register'));
const Login = lazy(() => import('./User/Login'));
const ProductDetailPage = lazy(() => import('./Catalog/ProductDetail'));
const ProductReviewsPage = lazy(() => import('./Catalog/ProductReviews'));
const WishListPage = lazy(() => import('./User/WishList'));
const CartPage = lazy(() => import('./User/Cart/index.tsx'));
const CheckoutPage = lazy(() => import('./User/Checkout'));
const MyOrdersPage = lazy(() => import('./User/MyOrders'));
const MyAccountPage = lazy(() => import('./User/MyAccount'));
const NewAddressPage = lazy(() => import('./User/NewAddress'));
const EditAddressPage = lazy(() => import('./User/EditAddress'));
const InventoryPage = lazy(() => import('./Inventory'));
const InventoryDetails = lazy(() => import('./Inventory/Details'));

const ForgotPasswordPage = lazy(() => import('./ForgotPassword'));
const ResetPasswordPage = lazy(() => import('./ResetPassword'));

const ProtectedRoute = ({ onlyFor }: { onlyFor: string[] }) => {
    const { user } = useAuth();

    if (!user || !user.roles.some((role) => onlyFor.includes(role))) {
        return <Navigate to="/catalog" replace />;
    }

    return <Outlet />;
};

const router = createBrowserRouter([
    {
        path: '/',
        element: <Root />,
        children: [
            { path: '', element: <CatalogPage /> },
            { path: 'about', element: <About /> },
            { path: 'register', element: <Register /> },
            { path: 'login', element: <Login /> },
            { path: 'catalog', element: <CatalogPage /> },
            { path: 'catalog/:id', element: <ProductDetailPage /> },
            { path: 'catalog/:id/reviews', element: <ProductReviewsPage /> },
            { path: 'forgot-password', element: <ForgotPasswordPage /> },
            { path: 'reset-password', element: <ResetPasswordPage /> },
            {
                path: 'user',
                element: <ProtectedRoute onlyFor={['User']} />,
                children: [
                    {
                        path: 'wishlist',
                        element: <WishListPage />,
                    },
                    {
                        path: 'cart',
                        element: <CartPage />,
                    },
                    {
                        path: 'cart/checkout',
                        element: <CheckoutPage />,
                    },
                    {
                        path: 'my-orders',
                        element: <MyOrdersPage />,
                    },
                    {
                        path: 'my-account',
                        element: <MyAccountPage />,
                    },
                    {
                        path: 'my-account/new-address',
                        element: <NewAddressPage />,
                    },
                    {
                        path: 'my-account/edit-address',
                        element: <EditAddressPage />,
                    },
                ],
            },
            {
                path: 'inventory',
                element: <ProtectedRoute onlyFor={['Super', 'Admin']} />,
                children: [
                    {
                        path: '',
                        element: <InventoryPage />,
                    },
                    {
                        path: ':id',
                        element: <InventoryDetails />,
                    },
                ],
            },
            { path: '*', element: <NotFoundPage /> },
        ],
    },
]);

export default router;
