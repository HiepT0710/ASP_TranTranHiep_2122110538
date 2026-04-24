import axios from "axios";

const baseURL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5208";

export const api = axios.create({
  baseURL,
  withCredentials: true,
});

export function toQueryString(params) {
  const query = new URLSearchParams();
  Object.entries(params || {}).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== "") {
      query.append(key, value);
    }
  });
  const text = query.toString();
  return text ? `?${text}` : "";
}
