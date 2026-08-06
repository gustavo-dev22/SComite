import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

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
                path: 'admin/institucion-educativa',
                loadComponent: () => import('./features/admin/ie/ie').then(m => m.IeComponent),
                data: { roles: ['Administrador'] },
                canActivate: [roleGuard]
            },
            {
                path: 'admin/periodos',
                loadComponent: () => import('./features/admin/periodo/periodo').then(m => m.PeriodoComponent),
                data: { roles: ['Administrador'] },
                canActivate: [roleGuard]
            },
            {
                path: 'admin/aulas',
                loadComponent: () => import('./features/admin/aula/aula').then(m => m.AulaComponent),
                data: { roles: ['Administrador', 'Comité de Aula'] },
                canActivate: [roleGuard]
            },
            {
                path: 'admin/asignacion-comite',
                loadComponent: () => import('./features/admin/asignacion-comite/asignacion-comite').then(m => m.AsignacionComiteComponent),
                data: { roles: ['Administrador', 'Comité de Aula'] },
                canActivate: [roleGuard]
            },
            {
                path: 'admin/estudiantes',
                loadComponent: () => import('./features/admin/padron-estudiantes/padron-estudiantes').then(m => m.PadronEstudiantesComponent),
                data: { roles: ['Administrador', 'Comité de Aula'] },
                canActivate: [roleGuard]
            },
            {
                path: 'admin/auditoria-cajas',
                loadComponent: () => import('./features/admin/resumen-general-cajas/resumen-general-cajas').then(m => m.ResumenGeneralCajasComponent),
                data: { roles: ['Administrador'] },
                canActivate: [roleGuard]
            },
            {
                path: 'admin/logs',
                loadComponent: () => import('./features/admin/logss/logs').then(m => m.LogsComponent),
                data: { roles: ['Administrador'] },
                canActivate: [roleGuard]
            },
            {
                path: 'seguridad/mantenimiento',
                loadComponent: () => import('./features/admin/mantenimiento-sistema/mantenimiento-sistema').then(m => m.MantenimientoSistemaComponent),
                data: { roles: ['Administrador'] },
                canActivate: [roleGuard]
            },
            {
                path: 'mis-pagos',
                loadComponent: () => import('./features/apoderado/mis-pagos/mis-pagos').then(m => m.MisPagosComponent),
                data: { roles: ['Administrador', 'Apoderado'] },
                canActivate: [roleGuard]
            },
            {
                path: 'tesoreria/cuotas',
                loadComponent: () => import('./features/comite/gestion-cuotas/gestion-cuotas').then(m => m.GestionCuotasComponent),
                data: { roles: ['Administrador', 'Comité de Aula'] },
                canActivate: [roleGuard]
            },
            {
                path: 'tesoreria/validar-pagos',
                loadComponent: () => import('./features/comite/validar-comprobantes/validar-comprobantes').then(m => m.ValidarComprobantesComponent),
                data: { roles: ['Administrador', 'Comité de Aula'] },
                canActivate: [roleGuard]
            },
            {
                path: 'tesoreria/donaciones',
                loadComponent: () => import('./features/comite/gestion-donaciones/gestion-donaciones').then(m => m.GestionDonacionesComponent),
                data: { roles: ['Administrador', 'Comité de Aula'] },
                canActivate: [roleGuard]
            },
            {
                path: 'tesoreria/gastos',
                loadComponent: () => import('./features/comite/registro-gastos/registro-gastos').then(m => m.RegistroGastosComponent),
                data: { roles: ['Administrador', 'Comité de Aula'] },
                canActivate: [roleGuard]
            },
            {
                path: 'tesoreria/balance',
                loadComponent: () => import('./features/comite/balance-caja/balance-caja').then(m => m.BalanceCajaComponent),
                data: { roles: ['Administrador', 'Comité de Aula'] },
                canActivate: [roleGuard]
            },
            {
                path: 'actividades/cronograma',
                loadComponent: () => import('./features/comite/cronograma-actividades/cronograma-actividades').then(m => m.CronogramaActividadesComponent),
                data: { roles: ['Administrador', 'Comité de Aula'] },
                canActivate: [roleGuard]
            },
            {
                path: 'comunidad/anuncios',
                loadComponent: () => import('./features/comite/muro-anuncios/muro-anuncios').then(m => m.MuroAnunciosComponent),
                data: { roles: ['Administrador', 'Comité de Aula'] },
                canActivate: [roleGuard]
            },
            {
                path: 'comunidad/actas',
                loadComponent: () => import('./features/comite/actas-asamblea/actas-asamblea').then(m => m.ActasAsambleaComponent),
                data: { roles: ['Administrador', 'Comité de Aula'] },
                canActivate: [roleGuard]
            },
            {
                path: 'aula/anuncios',
                loadComponent: () => import('./features/apoderado/muros-comunicados/muros-comunicados').then(m => m.MurosComunicadosComponent),
                data: { roles: ['Administrador', 'Apoderado'] },
                canActivate: [roleGuard]
            },
            {
                path: 'aula/cronograma',
                loadComponent: () => import('./features/apoderado/cronograma-eventos/cronograma-eventos').then(m => m.CronogramaEventosComponent),
                data: { roles: ['Administrador', 'Apoderado'] },
                canActivate: [roleGuard]
            },
            {
                path: 'aula/documentos',
                loadComponent: () => import('./features/apoderado/actas-documentos/actas-documentos').then(m => m.ActasDocumentosComponent),
                data: { roles: ['Administrador', 'Apoderado'] },
                canActivate: [roleGuard]
            },
            {
                path: 'aula/transparencia-balance',
                loadComponent: () => import('./features/apoderado/transparencia-balance/transparencia-balance').then(m => m.TransparenciaBalanceComponent),
                data: { roles: ['Administrador', 'Apoderado'] },
                canActivate: [roleGuard]
            }
        ]
    },
    {
        path: '**',
        redirectTo: 'login'
    }
];
