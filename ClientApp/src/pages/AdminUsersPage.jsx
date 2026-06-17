import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { ApiError, getUsers } from '../api/client.js';

export default function AdminUsersPage() {
  const [users, setUsers] = useState([]);
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;

    async function loadUsers() {
      setIsLoading(true);
      setError('');
      try {
        const data = await getUsers();
        if (isMounted) {
          setUsers(data || []);
        }
      } catch (requestError) {
        if (isMounted) {
          setError(requestError instanceof ApiError ? requestError.message : 'Unable to load users.');
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    }

    loadUsers();
    return () => {
      isMounted = false;
    };
  }, []);

  return (
    <section className="app-shell p-4">
      <h1 className="h3 mb-3">User Management</h1>

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}

      {isLoading ? (
        <div className="text-muted">Loading users...</div>
      ) : users.length === 0 ? (
        <div className="text-muted">No users found.</div>
      ) : (
        <div className="table-responsive">
          <table className="table table-striped align-middle mb-0">
            <thead>
              <tr>
                <th>Email</th>
                <th>Name</th>
                <th>Surname</th>
                <th>Phone</th>
                <th className="text-end">Actions</th>
              </tr>
            </thead>
            <tbody>
              {users.map((user) => (
                <tr key={user.id}>
                  <td>{user.email}</td>
                  <td>{user.name}</td>
                  <td>{user.surname}</td>
                  <td>{user.phoneNumber}</td>
                  <td className="table-actions text-end">
                    <Link className="btn btn-sm btn-outline-primary" to={`/admin/users/${user.id}`}>
                      Edit
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
