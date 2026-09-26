import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  template: `
    <header class="topbar">
      <a routerLink="/" class="brand">Encuesta System</a>
      @if (auth.isAuthenticated()) {
        <nav>
          <a routerLink="/encuestas/nueva" class="btn btn-primary">Nueva encuesta</a>
          <button type="button" class="btn" (click)="logout()">Salir</button>
        </nav>
      }
    </header>
    <main class="container">
      <router-outlet />
    </main>
  `,
})
export class App {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}
