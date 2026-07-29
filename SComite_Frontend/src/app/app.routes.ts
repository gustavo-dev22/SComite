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
                path: 'admin/periodos',
                loadComponent: () => import('./features/admin/periodo/periodo').then(m => m.PeriodoComponent)
            },
            {
                path: 'admin/aulas',
                loadComponent: () => import('./features/admin/aula/aula').then(m => m.AulaComponent)
            },
            {
                path: 'admin/asignacion-comite',
                loadComponent: () => import('./features/admin/asignacion-comite/asignacion-comite').then(m => m.AsignacionComiteComponent)
            },
            {
                path: 'admin/estudiantes',
                loadComponent: () => import('./features/admin/padron-estudiantes/padron-estudiantes').then(m => m.PadronEstudiantesComponent)
            },
            {
                path: 'admin/logs',
                loadComponent: () => import('./features/admin/logss/logs').then(m => m.LogsComponent)
            },
            {
                path: 'mis-pagos',
                loadComponent: () => import('./features/apoderado/mis-pagos/mis-pagos').then(m => m.MisPagosComponent)
            },
            {
                path: 'tesoreria/cuotas',
                loadComponent: () => import('./features/tesoreria/cuotas/cuotas').then(m => m.CuotasComponent)
            }
        ]
    },
    {
        path: '**',
        redirectTo: 'login'
    }
];
