import { Link } from "react-router-dom";

export function NotFoundPage() {
  return (
    <div className="auth-page">
      <div className="card">
        <h1>Page not found</h1>
        <p className="muted">The page you are looking for does not exist.</p>
        <Link className="btn btn-primary" to="/">
          Go home
        </Link>
      </div>
    </div>
  );
}
