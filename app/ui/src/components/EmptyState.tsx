interface EmptyStateProps {
  title: string;
  subtitle: string;
}

export default function EmptyState({ title, subtitle }: EmptyStateProps) {
  return (
    <div className="empty-state">
      <div className="empty-paw" aria-hidden="true">
        <svg viewBox="0 0 100 100" fill="currentColor" xmlns="http://www.w3.org/2000/svg">
          <ellipse cx="50" cy="70" rx="22" ry="18" />
          <ellipse cx="22" cy="44" rx="10" ry="12" transform="rotate(-15 22 44)" />
          <ellipse cx="39" cy="33" rx="10" ry="12" transform="rotate(-5 39 33)" />
          <ellipse cx="61" cy="33" rx="10" ry="12" transform="rotate(5 61 33)" />
          <ellipse cx="78" cy="44" rx="10" ry="12" transform="rotate(15 78 44)" />
        </svg>
      </div>
      <p className="empty-title">{title}</p>
      <p className="empty-sub">{subtitle}</p>
    </div>
  );
}
