import { Routes } from '@angular/router';
import { authGuard } from './core/auth';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Ingresar · Encuesta',
    loadComponent: () => import('./features/auth/login').then((m) => m.Login),
  },
  {
    path: 'e/:token',
    title: 'Responder encuesta · Encuesta',
    loadComponent: () =>
      import('./features/encuestas/public-respuesta').then((m) => m.PublicRespuesta),
  },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        title: 'Inicio · Encuesta',
        loadComponent: () => import('./features/encuestas/home').then((m) => m.Home),
      },
      {
        path: 'encuestas/nueva',
        title: 'Nueva encuesta · Encuesta',
        loadComponent: () => import('./features/encuestas/encuesta-form').then((m) => m.EncuestaForm),
      },
      {
        path: 'encuestas/:id',
        title: 'Detalle de encuesta · Encuesta',
        loadComponent: () =>
          import('./features/encuestas/encuesta-detail').then((m) => m.EncuestaDetail),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
