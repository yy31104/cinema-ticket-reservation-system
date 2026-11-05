import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  imports: [RouterOutlet, RouterLink],
  selector: 'app-root',
  template: `
    <header class="app-header">
      <a routerLink="/" class="brand">Cinema · Angular</a>
    </header>
    <main class="page">
      <router-outlet />
    </main>
  `,
})
export class App {}
