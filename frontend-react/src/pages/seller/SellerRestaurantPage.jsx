import { useEffect, useMemo, useState } from "react";
import { useToast } from "../../context/ToastContext";
import { getSellerRestaurant, resolveImageUrl, updateSellerRestaurantImages } from "../../services/apiService";

const imageKeys = [
  { key: "coverImage", label: "Ảnh bìa", clearKey: "clearCoverImage" },
  { key: "galleryImage1", label: "Gallery 1", clearKey: "clearGalleryImage1" },
  { key: "galleryImage2", label: "Gallery 2", clearKey: "clearGalleryImage2" },
  { key: "galleryImage3", label: "Gallery 3", clearKey: "clearGalleryImage3" },
];

export default function SellerRestaurantPage() {
  const { pushToast } = useToast();
  const [restaurant, setRestaurant] = useState(null);
  const [files, setFiles] = useState({ coverImage: null, galleryImage1: null, galleryImage2: null, galleryImage3: null });
  const [removeFlags, setRemoveFlags] = useState({ clearCoverImage: false, clearGalleryImage1: false, clearGalleryImage2: false, clearGalleryImage3: false });
  const [loading, setLoading] = useState(true);

  const load = async () => {
    setLoading(true);
    try {
      const data = await getSellerRestaurant();
      setRestaurant(data);
    } catch (e) {
      pushToast(e?.response?.data?.message || "Không tải được quán của bạn", "error");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const previews = useMemo(() => ({
    coverImage: files.coverImage ? URL.createObjectURL(files.coverImage) : restaurant?.coverImage,
    galleryImage1: files.galleryImage1 ? URL.createObjectURL(files.galleryImage1) : restaurant?.galleryImage1,
    galleryImage2: files.galleryImage2 ? URL.createObjectURL(files.galleryImage2) : restaurant?.galleryImage2,
    galleryImage3: files.galleryImage3 ? URL.createObjectURL(files.galleryImage3) : restaurant?.galleryImage3,
  }), [files, restaurant]);

  const onPick = (key, file) => setFiles((current) => ({ ...current, [key]: file || null }));

  const clearImage = (key) => {
    setFiles((current) => ({ ...current, [key]: null }));
    setRemoveFlags((current) => ({ ...current, [`clear${key[0].toUpperCase()}${key.slice(1)}`]: true }));
  };

  const save = async () => {
    try {
      await updateSellerRestaurantImages({
        ...files,
        ...removeFlags,
      });
      pushToast("Đã cập nhật ảnh quán", "success");
      setFiles({ coverImage: null, galleryImage1: null, galleryImage2: null, galleryImage3: null });
      setRemoveFlags({ clearCoverImage: false, clearGalleryImage1: false, clearGalleryImage2: false, clearGalleryImage3: false });
      await load();
    } catch (e) {
      pushToast(e?.response?.data?.message || "Không cập nhật được ảnh quán", "error");
    }
  };

  if (loading) return <section className="page">Đang tải quán của bạn...</section>;

  return (
    <section className="page hero-card">
      <div className="page-header">
        <div>
          <p className="eyebrow">Seller workspace</p>
          <h2>Ảnh quán của tôi</h2>
          <p className="muted">Tải ảnh đại diện và 3 ảnh gallery cho quán.</p>
        </div>
      </div>

      <div className="panel soft-panel">
        <h3>{restaurant?.name}</h3>
        <p className="muted">Trạng thái: {restaurant?.status}</p>
        <div className="restaurant-upload-grid">
          {imageKeys.map(({ key, label, clearKey }) => (
            <div key={key} className="upload-card">
              <span>{label}</span>
              <input type="file" accept="image/*" onChange={(e) => onPick(key, e.target.files?.[0] || null)} />
              <img src={resolveImageUrl(previews[key])} alt={label} />
              <div className="row">
                <button type="button" className="secondary" onClick={() => clearImage(key)}>Xóa ảnh này</button>
                {removeFlags[clearKey] && <span className="badge">Sẽ xóa khi lưu</span>}
              </div>
            </div>
          ))}
        </div>
        <div className="row" style={{ marginTop: 16 }}>
          <button onClick={save}>Lưu ảnh quán</button>
        </div>
      </div>
    </section>
  );
}
