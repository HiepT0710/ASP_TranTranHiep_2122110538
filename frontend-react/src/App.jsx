import { Navigate, Route, Routes } from "react-router-dom";
import Layout from "./components/Layout";
import ProtectedRoute from "./components/ProtectedRoute";
import AdminDashboardPage from "./pages/admin/AdminDashboardPage";
import AdminFoodFormPage from "./pages/admin/AdminFoodFormPage";
import AdminFoodsPage from "./pages/admin/AdminFoodsPage";
import AdminOrdersPage from "./pages/admin/AdminOrdersPage";
import AdminRestaurantsPage from "./pages/admin/AdminRestaurantsPage";
import AdminUsersPage from "./pages/admin/AdminUsersPage";
import CartPage from "./pages/CartPage";
import FoodsPage from "./pages/FoodsPage";
import FoodDetailsPage from "./pages/FoodDetailsPage";
import HomePage from "./pages/HomePage";
import LoginPage from "./pages/LoginPage";
import OrderDetailsPage from "./pages/OrderDetailsPage";
import OrdersPage from "./pages/OrdersPage";
import ProfilePage from "./pages/ProfilePage";
import RegisterPage from "./pages/RegisterPage";
import RestaurantDetailsPage from "./pages/RestaurantDetailsPage";
import RestaurantsPage from "./pages/RestaurantsPage";
import SellerCategoriesPage from "./pages/seller/SellerCategoriesPage";
import SellerDashboardPage from "./pages/seller/SellerDashboardPage";
import SellerFoodFormPage from "./pages/seller/SellerFoodFormPage";
import SellerFoodsPage from "./pages/seller/SellerFoodsPage";
import SellerOrdersPage from "./pages/seller/SellerOrdersPage";
import SellerPromotionsPage from "./pages/seller/SellerPromotionsPage";
import SellerRestaurantPage from "./pages/seller/SellerRestaurantPage";
import AdminPromotionsPage from "./pages/admin/AdminPromotionsPage";
import AdminPromotionDetailsPage from "./pages/admin/AdminPromotionDetailsPage";
import SellerPromotionDetailsPage from "./pages/seller/SellerPromotionDetailsPage";

export default function App() {
  return (
    <Layout>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/restaurants" element={<RestaurantsPage />} />
        <Route path="/restaurants/:id" element={<RestaurantDetailsPage />} />
        <Route path="/foods" element={<FoodsPage />} />
        <Route path="/foods/:id" element={<FoodDetailsPage />} />
        <Route path="/cart" element={<CartPage />} />

        <Route
          path="/orders"
          element={
            <ProtectedRoute roles={["User", "Seller", "Admin"]}>
              <OrdersPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/orders/:id"
          element={
            <ProtectedRoute roles={["User", "Seller", "Admin"]}>
              <OrderDetailsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/profile"
          element={
            <ProtectedRoute roles={["User", "Seller", "Admin"]}>
              <ProfilePage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/seller"
          element={
            <ProtectedRoute roles={["Seller"]}>
              <SellerDashboardPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/seller/categories"
          element={
            <ProtectedRoute roles={["Seller"]}>
              <SellerCategoriesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/seller/restaurant"
          element={
            <ProtectedRoute roles={["Seller"]}>
              <SellerRestaurantPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/seller/foods"
          element={
            <ProtectedRoute roles={["Seller"]}>
              <SellerFoodsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/seller/foods/new"
          element={
            <ProtectedRoute roles={["Seller"]}>
              <SellerFoodFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/seller/foods/:id/edit"
          element={
            <ProtectedRoute roles={["Seller"]}>
              <SellerFoodFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/seller/orders"
          element={
            <ProtectedRoute roles={["Seller"]}>
              <SellerOrdersPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/seller/promotions"
          element={
            <ProtectedRoute roles={["Seller"]}>
              <SellerPromotionsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/seller/promotions/:id"
          element={
            <ProtectedRoute roles={["Seller"]}>
              <SellerPromotionDetailsPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/admin"
          element={
            <ProtectedRoute roles={["Admin"]}>
              <AdminDashboardPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/users"
          element={
            <ProtectedRoute roles={["Admin"]}>
              <AdminUsersPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/restaurants"
          element={
            <ProtectedRoute roles={["Admin"]}>
              <AdminRestaurantsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/orders"
          element={
            <ProtectedRoute roles={["Admin"]}>
              <AdminOrdersPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/foods"
          element={
            <ProtectedRoute roles={["Admin"]}>
              <AdminFoodsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/foods/new"
          element={
            <ProtectedRoute roles={["Admin"]}>
              <AdminFoodFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/foods/:id/edit"
          element={
            <ProtectedRoute roles={["Admin"]}>
              <AdminFoodFormPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/promotions"
          element={
            <ProtectedRoute roles={["Admin"]}>
              <AdminPromotionsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/promotions/:id"
          element={
            <ProtectedRoute roles={["Admin"]}>
              <AdminPromotionDetailsPage />
            </ProtectedRoute>
          }
        />

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </Layout>
  );
}
