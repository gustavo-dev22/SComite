import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
    {
        path: '',
        redirectTo: 'login',
        pathMatch: 'full'
    },
        {
        path: 'login',
        loadComponent: () => import('./features/auth/login/login').then(m => m.LoginComponent)
    },
    {
        path: '',
        loadComponent: () => import('./shared/components/layout/layout').then(m => m.LayoutComponent),
        canActivate: [authGuard],
        children: [
            {
                path: 'admin/aulas',
                loadComponent: () => import('./features/admin/aula/aula').then(m => m.AulaComponent)
            },
        ]
    },
    {
        path: '**',
        redirectTo: 'login'
    }
];
