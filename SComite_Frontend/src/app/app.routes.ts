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
                path: 'admin/institucion-educativa',
                loadComponent: () => import('./features/admin/ie/ie').then(m => m.IeComponent)
            },
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
                path: 'admin/auditoria-cajas',
                loadComponent: () => import('./features/admin/resumen-general-cajas/resumen-general-cajas').then(m => m.ResumenGeneralCajasComponent)
            },
            {
                path: 'admin/logs',
                loadComponent: () => import('./features/admin/logss/logs').then(m => m.LogsComponent)
            },
            {
                path: 'seguridad/mantenimiento',
                loadComponent: () => import('./features/admin/mantenimiento-sistema/mantenimiento-sistema').then(m => m.MantenimientoSistemaComponent)
            },
            {
                path: 'mis-pagos',
                loadComponent: () => import('./features/apoderado/mis-pagos/mis-pagos').then(m => m.MisPagosComponent)
            },
            {
                path: 'tesoreria/cuotas',
                loadComponent: () => import('./features/comite/gestion-cuotas/gestion-cuotas').then(m => m.GestionCuotasComponent)
            },
            {
                path: 'tesoreria/validar-pagos',
                loadComponent: () => import('./features/comite/validar-comprobantes/validar-comprobantes').then(m => m.ValidarComprobantesComponent)
            },
            {
                path: 'tesoreria/donaciones',
                loadComponent: () => import('./features/comite/gestion-donaciones/gestion-donaciones').then(m => m.GestionDonacionesComponent)
            },
            {
                path: 'tesoreria/gastos',
                loadComponent: () => import('./features/comite/registro-gastos/registro-gastos').then(m => m.RegistroGastosComponent)
            },
            {
                path: 'tesoreria/balance',
                loadComponent: () => import('./features/comite/balance-caja/balance-caja').then(m => m.BalanceCajaComponent)
            },
            {
                path: 'actividades/cronograma',
                loadComponent: () => import('./features/comite/cronograma-actividades/cronograma-actividades').then(m => m.CronogramaActividadesComponent)
            },
            {
                path: 'comunidad/anuncios',
                loadComponent: () => import('./features/comite/muro-anuncios/muro-anuncios').then(m => m.MuroAnunciosComponent)
            },
            {
                path: 'comunidad/actas',
                loadComponent: () => import('./features/comite/actas-asamblea/actas-asamblea').then(m => m.ActasAsambleaComponent)
            },
            {
                path: 'aula/anuncios',
                loadComponent: () => import('./features/apoderado/muros-comunicados/muros-comunicados').then(m => m.MurosComunicadosComponent)
            },
            {
                path: 'aula/cronograma',
                loadComponent: () => import('./features/apoderado/cronograma-eventos/cronograma-eventos').then(m => m.CronogramaEventosComponent)
            },
            {
                path: 'aula/documentos',
                loadComponent: () => import('./features/apoderado/actas-documentos/actas-documentos').then(m => m.ActasDocumentosComponent)
            },
            {
                path: 'aula/transparencia-balance',
                loadComponent: () => import('./features/apoderado/transparencia-balance/transparencia-balance').then(m => m.TransparenciaBalanceComponent)
            }
        ]
    },
    {
        path: '**',
        redirectTo: 'login'
    }
];
