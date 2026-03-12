import { useAppContext } from '../context/AppContext';
import { PAGE_SIZE } from '../config/constants';

export default function Pagination() {
  const { page, setPage, totalCount } = useAppContext();

  const totalPages = Math.ceil(totalCount / PAGE_SIZE);
  if (totalPages <= 1) return null;

  const start = Math.max(1, page - 2);
  const end = Math.min(totalPages, page + 2);
  const pageNumbers: (number | 'dots')[] = [];

  if (start > 1) {
    pageNumbers.push(1);
    if (start > 2) pageNumbers.push('dots');
  }
  for (let i = start; i <= end; i++) pageNumbers.push(i);
  if (end < totalPages) {
    if (end < totalPages - 1) pageNumbers.push('dots');
    pageNumbers.push(totalPages);
  }

  const goToPage = (p: number) => {
    setPage(p);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  return (
    <div className="pagination">
      <button
        className="page-arrow"
        aria-label="Previous page"
        disabled={page <= 1}
        onClick={() => goToPage(page - 1)}
      >
        <svg
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <polyline points="15 18 9 12 15 6" />
        </svg>
      </button>
      <div className="page-numbers">
        {pageNumbers.map((item, idx) =>
          item === 'dots' ? (
            <span key={`dots-${idx}`} className="page-dots">
              ...
            </span>
          ) : (
            <button
              key={item}
              className={`page-num${item === page ? ' active' : ''}`}
              onClick={() => goToPage(item)}
            >
              {item}
            </button>
          ),
        )}
      </div>
      <span className="page-info">
        Page {page} of {totalPages}
      </span>
      <button
        className="page-arrow"
        aria-label="Next page"
        disabled={page >= totalPages}
        onClick={() => goToPage(page + 1)}
      >
        <svg
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <polyline points="9 6 15 12 9 18" />
        </svg>
      </button>
    </div>
  );
}
