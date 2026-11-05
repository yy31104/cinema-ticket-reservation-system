import { Routes } from '@angular/router';
import { ScreeningsPage } from './screenings-page';
import { SeatMapPage } from './seat-map-page';

export const routes: Routes = [
  { path: '', component: ScreeningsPage, title: 'Screenings' },
  { path: 'screenings/:id', component: SeatMapPage, title: 'Choose seats' },
  { path: '**', redirectTo: '' },
];
