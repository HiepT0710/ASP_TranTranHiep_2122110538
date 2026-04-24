export function getApiErrorMessage(error, fallback = "Có lỗi xảy ra") {
  const data = error?.response?.data;
  if (!data) return fallback;

  if (typeof data === "string") return data;
  if (typeof data.message === "string" && data.message.trim()) return data.message;
  if (typeof data.title === "string" && data.title.trim()) return data.title;

  // ASP.NET ModelState: { errors: { Field: ["msg1", ...] } }
  if (data.errors && typeof data.errors === "object") {
    const firstGroup = Object.values(data.errors).find((arr) => Array.isArray(arr) && arr.length > 0);
    if (firstGroup) return firstGroup.join(" | ");
  }

  return fallback;
}
