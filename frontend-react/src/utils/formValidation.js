export function isValidPhone(phone) {
  return /^\d{9,11}$/.test(String(phone || "").trim());
}

export function passwordStrength(password) {
  const value = String(password || "");
  const hasLetter = /[A-Za-z]/.test(value);
  const hasNumber = /\d/.test(value);
  const longEnough = value.length >= 8;
  if (!value) return { level: "empty", label: "Chưa nhập", ok: false };
  if (hasLetter && hasNumber && longEnough) return { level: "strong", label: "Mạnh", ok: true };
  if (hasLetter && hasNumber) return { level: "medium", label: "Trung bình", ok: false };
  return { level: "weak", label: "Yếu", ok: false };
}

export function validateRequired(fields, labels) {
  for (const field of fields) {
    if (!String(field.value || "").trim()) return labels[field.key] || field.key;
  }
  return "";
}
