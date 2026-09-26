import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

const STORAGE_KEY = 'encuesta.token';

const readStored = (): string | null => {
  try {
    return sessionStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
};

/** Lee un claim del payload de un JWT (sin verificar la firma: eso lo hace la API). */
export const readClaim = (token: string, claim: string): string | null => {
  try {
    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const value = (JSON.parse(atob(payload)) as Record<string, unknown>)[claim];
    return typeof value === 'string' ? value : null;
  } catch {
    return null;
  }
};

export const looksLikeJwt = (token: string): boolean =>
  /^[\w-]+\.[\w-]+\.[\w-]*$/.test(token.trim());

/** Guarda el JWT emitido por el proveedor OIDC. La API valida la firma y el vencimiento. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _token = signal<string | null>(readStored());

  readonly isAuthenticated = computed(() => this._token() !== null);
  readonly userId = computed(() => {
    const token = this._token();
    return token ? readClaim(token, 'sub') : null;
  });

  get token(): string | null {
    return this._token();
  }

  login(token: string): void {
    const clean = token.trim();
    this._token.set(clean);
    try {
      sessionStorage.setItem(STORAGE_KEY, clean);
    } catch {
      /* almacenamiento no disponible: la sesión vive solo en memoria */
    }
  }

  logout(): void {
    this._token.set(null);
    try {
      sessionStorage.removeItem(STORAGE_KEY);
    } catch {
      /* nada que limpiar */
    }
  }
}

export const authGuard: CanActivateFn = () => {
  const router = inject(Router);
  return inject(AuthService).isAuthenticated() ? true : router.createUrlTree(['/login']);
};

/** Añade el Bearer a las llamadas a /api y cierra sesión ante un 401. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.token;

  const authorized =
    token && req.url.startsWith('/api')
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req;

  return next(authorized).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        auth.logout();
        void router.navigate(['/login']);
      }
      return throwError(() => error);
    }),
  );
};
