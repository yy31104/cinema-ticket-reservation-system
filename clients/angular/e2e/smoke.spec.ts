import { APIRequestContext, expect, request, test } from '@playwright/test';
import { smokeAdmin } from './smoke-admin';

const password = 'Passw0rd!';
const run = Date.now();
const film = `Smoke Film ${run}`;
const userEmail = `smoke-user-${run}@example.test`;
let screeningId: number;

async function signIn(api: APIRequestContext, email: string, secret: string): Promise<{ isAdmin: boolean }> {
  const response = await api.post('/api/auth/login', { data: { email, password: secret, rememberMe: false } });
  expect(response.status(), await response.text()).toBe(200);
  return response.json();
}

async function register(api: APIRequestContext, email: string): Promise<{ isAdmin: boolean }> {
  const response = await api.post('/api/auth/register', {
    data: { email, password, confirmPassword: password, name: 'Smoke', surname: 'Test' },
  });
  expect(response.status(), await response.text()).toBe(201);
  return response.json();
}

test.beforeAll(async ({ baseURL }) => {
  // The admin was pre-created by start-backend.sh; the tests only sign in.
  const admin = await request.newContext({ baseURL });
  const adminUser = await signIn(admin, smokeAdmin.email, smokeAdmin.password);
  expect(adminUser.isAdmin, 'start-backend.sh should have pre-created this admin').toBe(true);

  const created = await admin.post('/api/screenings', {
    data: {
      filmTitle: film,
      startTime: `${new Date().getFullYear() + 1}-01-15T19:30:00`,
      cinemaId: 1,
      durationMinutes: 118,
    },
  });
  expect(created.status(), await created.text()).toBe(201);
  screeningId = (await created.json()).id;
  await admin.dispose();

  const user = await request.newContext({ baseURL });
  const regular = await register(user, userEmail);
  expect(regular.isAdmin, 'only the pre-created account should be admin').toBe(false);
  await user.dispose();
});

test('screenings page lists the screening and links to its seat map', async ({ page }) => {
  await page.goto('/');

  const card = page.getByTestId('screening-card').filter({ hasText: film });
  await expect(card).toContainText('City Center Cinema');
  await expect(card).toContainText('96 of 96 seats free');

  await card.getByRole('link', { name: film }).click();
  await expect(page).toHaveURL(new RegExp(`/screenings/${screeningId}$`));
  await expect(page.getByRole('heading', { name: 'Sign in to choose seats' })).toBeVisible();
});

test('signed-in user reserves a seat and it survives a reload', async ({ page }) => {
  await page.goto(`/screenings/${screeningId}`);
  await page.getByLabel('Email').fill(userEmail);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Sign in' }).click();

  await expect(page.getByRole('heading', { name: film })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Select a seat' })).toBeDisabled();

  await page.getByRole('gridcell', { name: 'Row 3, seat 5, free' }).click();
  await page.getByRole('button', { name: 'Reserve row 3 seat 5' }).click();

  await expect(page.getByRole('status')).toHaveText('Reserved row 3 seat 5.');
  await expect(page.getByRole('gridcell', { name: 'Row 3, seat 5, yours' })).toBeDisabled();

  // The reservation lives in the backend, not in component state.
  await page.reload();
  await expect(page.getByRole('gridcell', { name: 'Row 3, seat 5, yours' })).toBeVisible();

  await page.goto('/');
  await expect(page.getByTestId('screening-card').filter({ hasText: film })).toContainText('95 of 96 seats free');
});
