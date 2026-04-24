import { api, toQueryString } from "../api";

export function resolveImageUrl(image) {
  if (!image) return "";
  if (image.startsWith("http://") || image.startsWith("https://")) return image;
  if (image.startsWith("/")) return `${api.defaults.baseURL}${image}`;
  return `${api.defaults.baseURL}/${image.replace(/^\/+/, "")}`;
}

export async function getMe() {
  const res = await api.get("/Account/Me");
  return res.data;
}

export async function login(payload) {
  const res = await api.post("/Account/Login", payload);
  return res.data;
}

export async function logout() {
  const res = await api.post("/Account/Logout", {});
  return res.data;
}

export async function registerUser(payload) {
  const res = await api.post("/Account/Register", payload);
  return res.data;
}

export async function registerSeller(payload) {
  const res = await api.post("/Account/RegisterSeller", payload);
  return res.data;
}

export async function getProfile() {
  const res = await api.get("/Account/Profile");
  return res.data;
}

export async function updateProfile(payload) {
  const res = await api.put("/Account/Profile", payload);
  return res.data;
}

export async function getRestaurants(query = {}) {
  const res = await api.get(`/Restaurant/Index${toQueryString(query)}`);
  return res.data;
}

export async function getSaleRestaurants() {
  const res = await api.get("/Restaurant/Sale");
  return res.data;
}

export async function getRestaurantDetails(id) {
  const res = await api.get(`/Restaurant/Details/${id}`);
  return res.data;
}

export async function getFoods(query = {}) {
  const res = await api.get(`/Food/Index${toQueryString(query)}`);
  return res.data;
}

export async function getBestFoods(take = 8) {
  const res = await api.get(`/Restaurant/BestSellers?take=${take}`);
  return res.data;
}

export async function getFoodDetails(id) {
  const res = await api.get(`/Food/Details/${id}`);
  return res.data;
}

export async function getSaleFoods() {
  const res = await api.get("/Food/Sale");
  return res.data;
}

export async function getFoodReviews(foodId, query = {}) {
  const res = await api.get(`/Food/Reviews?foodId=${foodId}${toQueryString(query).replace("?", "&")}`);
  return res.data;
}

export async function getFoodCategories(restaurantId) {
  const res = await api.get(`/Food/Categories?restaurantId=${restaurantId}`);
  return res.data;
}

export async function getCart() {
  const res = await api.get("/Cart/Index");
  return res.data;
}

export async function addToCart(payload) {
  const res = await api.post("/Cart/Add", payload);
  return res.data;
}

export async function updateCart(payload) {
  const res = await api.post("/Cart/Update", payload);
  return res.data;
}

export async function removeFromCart(foodId) {
  const res = await api.post("/Cart/Remove", { foodId });
  return res.data;
}

export async function clearCart() {
  const res = await api.post("/Cart/Clear", {});
  return res.data;
}

export async function checkout(payload) {
  const res = await api.post("/Order/Checkout", payload);
  return res.data;
}

export async function getMyOrders(query = {}) {
  const res = await api.get(`/Order/MyOrders${toQueryString(query)}`);
  return res.data;
}

export async function cancelOrder(id, reason) {
  const res = await api.post(`/Order/Cancel?id=${id}`, { reason });
  return res.data;
}

export async function getOrderDetails(id) {
  const res = await api.get(`/Order/Details/${id}`);
  return res.data;
}

export async function getOrderStatusHistory(id) {
  const res = await api.get(`/Order/StatusHistory/${id}`);
  return res.data;
}

export async function getOrderChatMessages(id, query = {}) {
  const res = await api.get(`/Order/ChatMessages/${id}${toQueryString(query)}`);
  return res.data;
}

export async function submitOrderReview(payload) {
  const res = await api.post("/Order/SubmitReview", payload);
  return res.data;
}

export async function getSellerSummary() {
  const res = await api.get("/Seller/Statistics/Summary");
  return res.data;
}

export async function getSellerCategories() {
  const res = await api.get("/Seller/Categories/Index");
  return res.data;
}

export async function getSellerCategoryDetails(id) {
  const res = await api.get(`/Seller/Categories/Details/${id}`);
  return res.data;
}

export async function createSellerCategory(payload) {
  const res = await api.post("/Seller/Categories/Create", payload);
  return res.data;
}

export async function updateSellerCategory(id, payload) {
  const res = await api.put(`/Seller/Categories/Edit/${id}`, payload);
  return res.data;
}

export async function deleteSellerCategory(id) {
  const res = await api.delete(`/Seller/Categories/Delete/${id}`);
  return res.data;
}

export async function getSellerFoods(query = {}) {
  const res = await api.get(`/Seller/Foods/Index${toQueryString(query)}`);
  return res.data;
}

export async function getSellerFoodDetails(id) {
  const res = await api.get(`/Seller/Foods/Details/${id}`);
  return res.data;
}

function toFormData(payload) {
  const formData = new FormData();
  Object.entries(payload).forEach(([key, value]) => {
    if (value !== undefined && value !== null) formData.append(key, value);
  });
  return formData;
}

export async function createSellerFood(payload) {
  const res = await api.post("/Seller/Foods/Create", toFormData(payload), {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return res.data;
}

export async function editSellerFood(id, payload) {
  const res = await api.put(`/Seller/Foods/Edit?id=${id}`, toFormData(payload), {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return res.data;
}

export async function deleteSellerFood(id) {
  const res = await api.delete(`/Seller/Foods/Delete?id=${id}`);
  return res.data;
}

export async function getSellerOrders(query = {}) {
  const res = await api.get(`/Seller/Orders/Index${toQueryString(query)}`);
  return res.data;
}

export async function sellerUpdateOrderStatus(id, payload) {
  const res = await api.post(`/Seller/Orders/UpdateStatus/${id}`, payload);
  return res.data;
}

export async function getAdminSummary() {
  const res = await api.get("/Admin/Statistics/Summary");
  return res.data;
}

export async function getAdminUsers(query = {}) {
  const res = await api.get(`/Admin/Users/Index${toQueryString(query)}`);
  return res.data;
}

export async function updateAdminUserRole(id, role) {
  const res = await api.put(`/Admin/Users/EditRole/${id}`, { role });
  return res.data;
}

export async function getAdminRestaurants(query = {}) {
  const res = await api.get(`/Admin/Restaurants/Index${toQueryString(query)}`);
  return res.data;
}

export async function adminRestaurantAction(id, action) {
  const res = await api.post(`/Admin/Restaurants/${action}/${id}`, {});
  return res.data;
}

export async function getAdminOrders(query = {}) {
  const res = await api.get(`/Admin/Orders/Index${toQueryString(query)}`);
  return res.data;
}

export async function getAdminFoods(query = {}) {
  const res = await api.get(`/Admin/Foods/Index${toQueryString(query)}`);
  return res.data;
}

export async function getAdminFoodDetails(id) {
  const res = await api.get(`/Admin/Foods/Details/${id}`);
  return res.data;
}

export async function createAdminFood(payload) {
  const res = await api.post("/Admin/Foods/Create", toFormData(payload), {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return res.data;
}

export async function editAdminFood(id, payload) {
  const res = await api.put(`/Admin/Foods/Edit?id=${id}`, toFormData(payload), {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return res.data;
}

export async function deleteAdminFood(id) {
  const res = await api.delete(`/Admin/Foods/Delete/${id}`);
  return res.data;
}

export async function getAdminCategories() {
  const res = await api.get("/Admin/Categories/Index");
  return res.data;
}

export async function adminUpdateOrder(id, status) {
  const res = await api.post(`/Admin/Orders/UpdateStatus/${id}`, { status });
  return res.data;
}

export async function getAdminPromotions() {
  const res = await api.get("/Admin/Promotions/Index");
  return res.data;
}

export async function getAdminPromotionDetails(id) {
  const res = await api.get(`/Admin/Promotions/Details/${id}`);
  return res.data;
}

export async function createAdminPromotion(payload) {
  const res = await api.post("/Admin/Promotions/Create", payload);
  return res.data;
}

export async function toggleAdminPromotion(id) {
  const res = await api.put(`/Admin/Promotions/Toggle/${id}`);
  return res.data;
}


export async function deleteAdminPromotion(id) {
  const res = await api.delete(`/Admin/Promotions/Delete/${id}`);
  return res.data;
}

export async function editAdminPromotion(id, payload) {
  const res = await api.put(`/Admin/Promotions/Edit/${id}`, payload);
  return res.data;
}

export async function editAdminVoucher(id, payload) {
  const res = await api.put(`/Admin/Vouchers/Edit/${id}`, payload);
  return res.data;
}

export async function getAdminVouchers(query = {}) {
  const res = await api.get(`/Admin/Vouchers/Index${toQueryString(query)}`);
  return res.data;
}

export async function createAdminVoucher(payload) {
  const res = await api.post("/Admin/Vouchers/Create", payload);
  return res.data;
}

export async function toggleAdminVoucher(id) {
  const res = await api.put(`/Admin/Vouchers/Toggle/${id}`);
  return res.data;
}


export async function deleteAdminVoucher(id) {
  const res = await api.delete(`/Admin/Vouchers/Delete/${id}`);
  return res.data;
}

export async function getSellerPromotions() {
  const res = await api.get("/Seller/Promotions/Index");
  return res.data;
}

export async function getSellerPromotionDetails(id) {
  const res = await api.get(`/Seller/Promotions/Details/${id}`);
  return res.data;
}

export async function createSellerPromotion(payload) {
  const res = await api.post("/Seller/Promotions/Create", payload);
  return res.data;
}

export async function editSellerPromotion(id, payload) {
  const res = await api.put(`/Seller/Promotions/Edit/${id}`, payload);
  return res.data;
}

export async function toggleSellerPromotion(id) {
  const res = await api.put(`/Seller/Promotions/Toggle/${id}`);
  return res.data;
}

export async function deleteSellerPromotion(id) {
  const res = await api.delete(`/Seller/Promotions/Delete/${id}`);
  return res.data;
}

export async function getSellerVouchers(query = {}) {
  const res = await api.get(`/Seller/Vouchers/Index${toQueryString(query)}`);
  return res.data;
}

export async function getSellerVoucherDetails(id) {
  const res = await api.get(`/Seller/Vouchers/Details/${id}`);
  return res.data;
}

export async function createSellerVoucher(payload) {
  const res = await api.post("/Seller/Vouchers/Create", payload);
  return res.data;
}

export async function editSellerVoucher(id, payload) {
  const res = await api.put(`/Seller/Vouchers/Edit/${id}`, payload);
  return res.data;
}

export async function toggleSellerVoucher(id) {
  const res = await api.put(`/Seller/Vouchers/Toggle/${id}`);
  return res.data;
}

export async function deleteSellerVoucher(id) {
  const res = await api.delete(`/Seller/Vouchers/Delete/${id}`);
  return res.data;
}

export async function getSellerRestaurant() {
  const res = await api.get("/Seller/Restaurants/My");
  return res.data;
}

export async function updateSellerRestaurantImages(payload) {
  const formData = new FormData();
  Object.entries(payload || {}).forEach(([key, value]) => {
    if (typeof value === "boolean") {
      if (value) formData.append(key, "true");
      return;
    }
    if (value !== undefined && value !== null && value !== "") formData.append(key, value);
  });
  const res = await api.put("/Seller/Restaurants/UpdateImages", formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return res.data;
}
