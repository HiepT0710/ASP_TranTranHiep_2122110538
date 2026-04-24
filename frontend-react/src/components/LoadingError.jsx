export function InlineError({ message, onRetry }) {
  return (
    <div className="panel soft-panel">
      <h3>Đã xảy ra lỗi</h3>
      <p className="muted">{message}</p>
      {onRetry && <button onClick={onRetry}>Thử lại</button>}
    </div>
  );
}
