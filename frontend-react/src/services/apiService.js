import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { api, toQueryString } from "../api";

const unwrap = (res) => res.data;

export const resolveImageUrl = (path) => {
  if (!path) return "";
  if (path.startsWith("http://") || path.startsWith("https://") || path.startsWith("data:")) return path;
  const base = import.meta.env.VITE_API_BASE_URL || "http://localhost:5208";
  return `${base}${path.startsWith("/") ? "" : "/"}${path}`;
};

export const createOrderNotificationConnection = () => {
  const baseUrl = import.meta.env.VITE_API_BASE_URL || "http://localhost:5208";
  return new HubConnectionBuilder()
    .withUrl(`${baseUrl}/hubs/order`, { withCredentials: true })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();
};

export const createOrderChatConnection = () => {
  const baseUrl = import.meta.env.VITE_API_BASE_URL || "http://localhost:5208";
  return new HubConnectionBuilder()
    .withUrl(`${baseUrl}/hubs/orderchat`, { withCredentials: true })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();
};

export const sendOrderChatMessage = async (connection, orderId, message, target = "seller") => {
  if (!connection) throw new Error("Chat connection is not ready");
  return connection.invoke("SendOrderMessage", Number(orderId), String(message || ""), String(target || "seller"));
};

export const getAdminSummary = async () => unwrap(await api.get(`/Admin/Statistics/Summary`));
export const getSystemSettings = async () => unwrap(await api.get(`/Order/SystemSettings`));
export const getAdminUsers = async (params = {}) => unwrap(await api.get(`/Admin/Users/Index${toQueryString(params)}`));
export const getAdminUserDetails = async (id) => unwrap(await api.get(`/Admin/Users/Details/${id}`));
export const updateAdminUserRole = async (id, role) => unwrap(await api.put(`/Admin/Users/EditRole/${id}`, { role }));
export const resetAdminUserRole = async (id) => unwrap(await api.post(`/Admin/Users/ResetRole/${id}`));
export const lockAdminUser = async (id, reason) => unwrap(await api.post(`/Admin/Users/Lock/${id}`, { reason }));
export const unlockAdminUser = async (id) => unwrap(await api.post(`/Admin/Users/Unlock/${id}`));
export const deleteAdminUser = async (id) => unwrap(await api.delete(`/Admin/Users/Delete/${id}`));

export const getAdminRestaurants = async (params = {}) => unwrap(await api.get(`/Admin/Restaurants/Index${toQueryString(params)}`));
export const getAdminRestaurantDetails = async (id) => unwrap(await api.get(`/Admin/Restaurants/Details/${id}`));
export const adminRestaurantAction = async (id, action, payload = {}) => unwrap(await api.post(`/Admin/Restaurants/${action}/${id}`, payload));
export const approveAdminRestaurant = async (id, payload) => adminRestaurantAction(id, "Approve", payload);
export const rejectAdminRestaurant = async (id, payload) => adminRestaurantAction(id, "Reject", payload);
export const suspendAdminRestaurant = async (id, payload) => adminRestaurantAction(id, "Suspend", payload);
export const reopenAdminRestaurant = async (id, payload) => adminRestaurantAction(id, "Reopen", payload);
export const deleteAdminRestaurant = async (id) => unwrap(await api.delete(`/Admin/Restaurants/Delete/${id}`));

export const getAdminCategories = async () => unwrap(await api.get(`/Admin/Categories/Index`));

export const getAdminFoods = async (params = {}) => unwrap(await api.get(`/Admin/Foods/Index${toQueryString(params)}`));
export const getAdminFoodDetails = async (id) => unwrap(await api.get(`/Admin/Foods/Details/${id}`));
export const createAdminFood = async (payload) => {
  const formData = new FormData();
  Object.entries(payload || {}).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== "") formData.append(key, value);
  });
  const res = await api.post(`/Admin/Foods/Create`, formData, { headers: { "Content-Type": "multipart/form-data" } });
  return res.data;
};
export const editAdminFood = async (id, payload) => {
  const formData = new FormData();
  Object.entries(payload || {}).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== "") formData.append(key, value);
  });
  const res = await api.put(`/Admin/Foods/Edit/${id}`, formData, { headers: { "Content-Type": "multipart/form-data" } });
  return res.data;
};
export const deleteAdminFood = async (id) => unwrap(await api.delete(`/Admin/Foods/Delete/${id}`));

export const getAdminPromotions = async () => unwrap(await api.get(`/Admin/Promotions/Index`));
export const getAdminPromotionDetails = async (id) => unwrap(await api.get(`/Admin/Promotions/Details/${id}`));
export const createAdminPromotion = async (payload) => unwrap(await api.post(`/Admin/Promotions/Create`, payload));
export const editAdminPromotion = async (id, payload) => unwrap(await api.put(`/Admin/Promotions/Edit/${id}`, payload));
export const toggleAdminPromotion = async (id) => unwrap(await api.post(`/Admin/Promotions/Toggle/${id}`));
export const deleteAdminPromotion = async (id) => unwrap(await api.delete(`/Admin/Promotions/Delete/${id}`));

export const getAdminVouchers = async () => unwrap(await api.get(`/Admin/Vouchers/Index`));
export const getAdminVoucherDetails = async (id) => unwrap(await api.get(`/Admin/Vouchers/Details/${id}`));
export const getAdminSettings = async () => unwrap(await api.get(`/Admin/Settings/Index`));
export const upsertAdminSetting = async (payload) => unwrap(await api.put(`/Admin/Settings/Upsert`, payload));
export const getAdminAuditLogs = async (params = {}) => unwrap(await api.get(`/Admin/Audit/Index${toQueryString(params)}`));
export const getAdminReports = async (params = {}) => unwrap(await api.get(`/Admin/Reports/Index${toQueryString(params)}`));
export const resolveAdminReport = async (id, payload) => unwrap(await api.post(`/Admin/Reports/Resolve/${id}`, payload));
export const createAdminVoucher = async (payload) => unwrap(await api.post(`/Admin/Vouchers/Create`, payload));
export const editAdminVoucher = async (id, payload) => unwrap(await api.put(`/Admin/Vouchers/Edit/${id}`, payload));
export const toggleAdminVoucher = async (id) => unwrap(await api.post(`/Admin/Vouchers/Toggle/${id}`));
export const deleteAdminVoucher = async (id) => unwrap(await api.delete(`/Admin/Vouchers/Delete/${id}`));

export const getAdminOrders = async (params = {}) => unwrap(await api.get(`/Admin/Orders/Index${toQueryString(params)}`));
export const adminUpdateOrder = async (id, status) => unwrap(await api.post(`/Admin/Orders/Update/${id}`, { status }));

export const getSellerSummary = async () => unwrap(await api.get(`/Seller/Statistics/Summary`));
export const getSellerDashboard = async (params = {}) => unwrap(await api.get(`/Seller/Dashboard/Summary${toQueryString(params)}`));
export const getSellerAuditLogs = async (params = {}) => unwrap(await api.get(`/Seller/Audit/Index${toQueryString(params)}`));
export const getSellerRestaurant = async () => unwrap(await api.get(`/Seller/Restaurants/My`));
export const getSellerRestaurantOperations = async () => unwrap(await api.get(`/Seller/RestaurantOperations/Overview`));
export const updateSellerRestaurantState = async (payload) => unwrap(await api.put(`/Seller/RestaurantOperations/UpdateState`, payload));
export const upsertSellerOperatingHour = async (payload) => unwrap(await api.put(`/Seller/RestaurantOperations/UpsertHours`, payload));
export const updateSellerRestaurantImages = async (payload) => {
  const formData = new FormData();
  Object.entries(payload || {}).forEach(([key, value]) => {
    if (value instanceof File) formData.append(key, value);
    else if (value !== undefined && value !== null) formData.append(key, value);
  });
  const res = await api.put(`/Seller/Restaurants/UpdateImages`, formData, { headers: { "Content-Type": "multipart/form-data" } });
  return res.data;
};
export const getSellerOrders = async (params = {}) => unwrap(await api.get(`/Seller/Orders/Index${toQueryString(params)}`));
export const sellerUpdateOrderStatus = async (id, payload) => unwrap(await api.post(`/Seller/Orders/UpdateStatus/${id}`, payload));
export const sellerRejectOrder = async (id, payload) => unwrap(await api.post(`/Seller/Orders/Reject/${id}`, payload));
export const getSellerPromotions = async () => unwrap(await api.get(`/Seller/Promotions/Index`));
export const getSellerPromotionDetails = async (id) => unwrap(await api.get(`/Seller/Promotions/Details/${id}`));
export const createSellerPromotion = async (payload) => unwrap(await api.post(`/Seller/Promotions/Create`, payload));
export const editSellerPromotion = async (id, payload) => unwrap(await api.put(`/Seller/Promotions/Edit/${id}`, payload));
export const updateSellerPromotion = editSellerPromotion;
export const toggleSellerPromotion = async (id) => unwrap(await api.put(`/Seller/Promotions/Toggle/${id}`));
export const deleteSellerPromotion = async (id) => unwrap(await api.delete(`/Seller/Promotions/Delete/${id}`));

export const getSellerVouchers = async (params = {}) => unwrap(await api.get(`/Seller/Vouchers/Index${toQueryString(params)}`));
export const getSellerVoucherDetails = async (id) => unwrap(await api.get(`/Seller/Vouchers/Details/${id}`));
export const createSellerVoucher = async (payload) => unwrap(await api.post(`/Seller/Vouchers/Create`, payload));
export const editSellerVoucher = async (id, payload) => unwrap(await api.put(`/Seller/Vouchers/Edit/${id}`, payload));
export const updateSellerVoucher = editSellerVoucher;
export const toggleSellerVoucher = async (id) => unwrap(await api.put(`/Seller/Vouchers/Toggle/${id}`));
export const deleteSellerVoucher = async (id) => unwrap(await api.delete(`/Seller/Vouchers/Delete/${id}`));

export const getSellerCategories = async () => unwrap(await api.get(`/Seller/Categories/Index`));
export const getSellerCategoryDetails = async (id) => unwrap(await api.get(`/Seller/Categories/Details/${id}`));
export const createSellerCategory = async (payload) => unwrap(await api.post(`/Seller/Categories/Create`, payload));
export const updateSellerCategory = async (id, payload) => unwrap(await api.put(`/Seller/Categories/Edit/${id}`, payload));
export const deleteSellerCategory = async (id) => unwrap(await api.delete(`/Seller/Categories/Delete/${id}`));

export const getSellerFoods = async (params = {}) => unwrap(await api.get(`/Seller/Foods/Index${toQueryString(params)}`));
export const getSellerFoodDetails = async (id) => unwrap(await api.get(`/Seller/Foods/Details/${id}`));
export const createSellerFood = async (payload) => {
  const formData = new FormData();
  Object.entries(payload || {}).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== "") formData.append(key, value);
  });
  const res = await api.post(`/Seller/Foods/Create`, formData, { headers: { "Content-Type": "multipart/form-data" } });
  return res.data;
};
export const editSellerFood = async (id, payload) => {
  const formData = new FormData();
  Object.entries(payload || {}).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== "") formData.append(key, value);
  });
  const res = await api.put(`/Seller/Foods/Edit/${id}`, formData, { headers: { "Content-Type": "multipart/form-data" } });
  return res.data;
};
export const updateSellerFood = editSellerFood;
export const updateSellerFoodFlags = async (id, payload) => unwrap(await api.put(`/Seller/Foods/UpdateFlags/${id}`, payload));
export const deleteSellerFood = async (id) => unwrap(await api.delete(`/Seller/Foods/Delete/${id}`));

export const getFoods = async (params = {}) => unwrap(await api.get(`/Food/Index${toQueryString(params)}`));
export const getFoodDetails = async (id) => unwrap(await api.get(`/Food/Details/${id}`));
export const getFoodReviews = async (foodIdOrParams = {}, maybeParams = {}) => {
  const params = typeof foodIdOrParams === "object" && foodIdOrParams !== null
    ? foodIdOrParams
    : { foodId: foodIdOrParams, ...maybeParams };
  return unwrap(await api.get(`/Food/Reviews${toQueryString(params)}`));
};
export const getFoodCategories = async (restaurantId) => unwrap(await api.get(`/Food/Categories${toQueryString({ restaurantId })}`));
export const getFoodCategoriesByRouteId = getFoodCategories;

export const getRestaurants = async (params = {}) => unwrap(await api.get(`/Restaurant/Index${toQueryString(params)}`));
export const getRestaurantDetails = async (id) => unwrap(await api.get(`/Restaurant/Details/${id}`));
export const getRestaurantReviews = async (params = {}) => unwrap(await api.get(`/Restaurant/Reviews${toQueryString(params)}`));
export const getRestaurantSale = async () => unwrap(await api.get(`/Restaurant/Sale`));
export const getSaleRestaurants = getRestaurantSale;
export const getBestSellers = async (take = 8) => unwrap(await api.get(`/Restaurant/BestSellers${toQueryString({ take })}`));
export const getBestFoods = getBestSellers;

export const getCart = async () => unwrap(await api.get(`/Cart/Index`));
export const addToCart = async (payload) => unwrap(await api.post(`/Cart/Add`, payload));
export const updateCart = async (payload) => unwrap(await api.post(`/Cart/Update`, payload));
export const removeFromCart = async (foodId) => unwrap(await api.post(`/Cart/Remove/${foodId}`));
export const clearCart = async () => unwrap(await api.post(`/Cart/Clear`));

export const checkout = async (payload) => unwrap(await api.post(`/Order/Checkout`, payload));
export const getVoucherSuggestions = async () => unwrap(await api.get(`/Order/VoucherSuggestions`));
export const simulateOnlinePayment = async (id) => unwrap(await api.post(`/Order/SimulateOnlinePayment/${id}`));
export const getMyOrders = async (params = {}) => unwrap(await api.get(`/Order/MyOrders${toQueryString(params)}`));
export const cancelOrder = async (id, reason) => unwrap(await api.post(`/Order/Cancel/${id}`, { reason }));
export const getOrderDetails = async (id) => unwrap(await api.get(`/Order/Details/${id}`));
export const getOrderStatusHistory = async (id) => unwrap(await api.get(`/Order/StatusHistory/${id}`));
export const getOrderChatMessages = async (id, params = {}) => unwrap(await api.get(`/Order/ChatMessages/${id}${toQueryString(params)}`));
export const submitOrderReview = async (payload) => unwrap(await api.post(`/Order/SubmitReview`, payload));
export const submitRestaurantReview = async (payload) => unwrap(await api.post(`/Order/SubmitRestaurantReview`, payload));
export const createModerationReport = async (payload) => unwrap(await api.post(`/Order/CreateReport`, payload));

export const getProfile = async () => unwrap(await api.get(`/Account/Profile`));
export const updateProfile = async (payload) => unwrap(await api.put(`/Account/Profile`, payload));
export const updateAvatar = async (file) => {
  const formData = new FormData();
  formData.append("Avatar", file);
  const res = await api.post(`/Account/Avatar`, formData, { headers: { "Content-Type": "multipart/form-data" } });
  return res.data;
};
export const changePassword = async (payload) => unwrap(await api.post(`/Account/ChangePassword`, payload));
export const getMe = async () => unwrap(await api.get(`/Account/Me`));
export const login = async (payload) => unwrap(await api.post(`/Account/Login`, payload));
export const logout = async () => unwrap(await api.post(`/Account/Logout`));
export const registerUser = async (payload) => unwrap(await api.post(`/Account/Register`, payload));
export const registerSeller = async (payload) => unwrap(await api.post(`/Account/RegisterSeller`, payload));
export const forgotPassword = async (payload) => unwrap(await api.post(`/Account/ForgotPassword`, payload));
export const resetPassword = async (payload) => unwrap(await api.post(`/Account/ResetPassword`, payload));

export const uploadReviewImage = async (file) => {
  const formData = new FormData();
  formData.append("file", file);
  const res = await api.post(`/Upload/ReviewImage`, formData, { headers: { "Content-Type": "multipart/form-data" } });
  return res.data;
};
