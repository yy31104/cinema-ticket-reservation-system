const API_BASE = '/api';

export class ApiError extends Error {
  constructor(message, status, details = null) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.details = details;
  }
}

async function parseResponse(response) {
  const contentType = response.headers.get('content-type') || '';
  if (!contentType.toLowerCase().includes('json')) {
    return null;
  }

  return response.json();
}

function extractErrorMessage(data, status) {
  if (!data) {
    return `Request failed with status ${status}.`;
  }

  if (typeof data === 'string') {
    return data;
  }

  if (data.message) {
    return data.message;
  }

  if (data.errors) {
    const validationMessages = Array.isArray(data.errors)
      ? data.errors
      : Object.values(data.errors).flat();

    if (validationMessages.length > 0) {
      return validationMessages.join(' ');
    }
  }

  if (data.title) {
    return data.title;
  }

  return `Request failed with status ${status}.`;
}

export async function apiRequest(path, options = {}) {
  const response = await fetch(`${API_BASE}${path}`, {
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json',
      ...options.headers
    },
    ...options
  });

  const data = await parseResponse(response);
  if (!response.ok) {
    throw new ApiError(extractErrorMessage(data, response.status), response.status, data);
  }

  return data;
}

export function getCurrentUser() {
  return apiRequest('/auth/me');
}

export function login(credentials) {
  return apiRequest('/auth/login', {
    method: 'POST',
    body: JSON.stringify(credentials)
  });
}

export function register(account) {
  return apiRequest('/auth/register', {
    method: 'POST',
    body: JSON.stringify(account)
  });
}

export function logout() {
  return apiRequest('/auth/logout', {
    method: 'POST'
  });
}

export function getScreenings() {
  return apiRequest('/screenings');
}

export function getScreening(id) {
  return apiRequest(`/screenings/${id}`);
}

export function getSeatMap(screeningId) {
  return apiRequest(`/screenings/${screeningId}/seats`);
}

export function reserveSeat(screeningId, seat) {
  return apiRequest(`/screenings/${screeningId}/reservations`, {
    method: 'POST',
    body: JSON.stringify(seat)
  });
}

export function cancelReservation(screeningId, reservationId) {
  return apiRequest(`/screenings/${screeningId}/reservations/${reservationId}`, {
    method: 'DELETE'
  });
}

export function deleteScreening(screeningId) {
  return apiRequest(`/screenings/${screeningId}`, {
    method: 'DELETE'
  });
}

export function getCinemas() {
  return apiRequest('/screenings/cinemas');
}

export function createScreening(screening) {
  return apiRequest('/screenings', {
    method: 'POST',
    body: JSON.stringify(screening)
  });
}

export function getProfile() {
  return apiRequest('/profile');
}

export function updateProfile(profile) {
  return apiRequest('/profile', {
    method: 'PUT',
    body: JSON.stringify({
      name: profile.name,
      surname: profile.surname,
      phoneNumber: profile.phoneNumber,
      rowVersion: profile.rowVersion ?? ''
    })
  });
}

export function getUsers() {
  return apiRequest('/users');
}

export function getUser(id) {
  return apiRequest(`/users/${id}`);
}

export function updateUser(id, user) {
  return apiRequest(`/users/${id}`, {
    method: 'PUT',
    body: JSON.stringify({
      name: user.name,
      surname: user.surname,
      phoneNumber: user.phoneNumber,
      rowVersion: user.rowVersion ?? ''
    })
  });
}

export function deleteUser(id, rowVersion) {
  return apiRequest(`/users/${id}?rowVersion=${encodeURIComponent(rowVersion ?? '')}`, {
    method: 'DELETE'
  });
}
