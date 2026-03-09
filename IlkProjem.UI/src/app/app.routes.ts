import { Routes } from '@angular/router';
import { Loginpage } from './layout/loginpage/loginpage';
import { Dashboard } from './features/dashboard/dashboard';
import { Mainpage } from './layout/mainpage/mainpage';
import { authGuard } from './core/guards/auth-guard';
import { RegistrationPage } from './layout/registrationpage/registrationpage';
import { loginGuard } from './core/guards/login-guard';
import { Landingpage } from './layout/landingpage/landingpage';
import { NotFound } from './layout/not-found/not-found';
import { Unauthorized } from './layout/unauthorized/unauthorized';
import { CustomerAssets } from './features/customer-assets/customer-assets';
import { AddCar } from './features/add-car/add-car';
import { AddHouse } from './features/add-house/add-house';
import { EditCar } from './features/edit-car/edit-car';
import { EditHouse } from './features/edit-house/edit-house';
import { SendEmail } from './features/send-email/send-email';
import { Settings } from './features/settings/settings';
import { UsersDisplay } from './features/users-display/users-display';
import { CustomerDisplay } from './features/customer-display/customer-display';
import { Transactions } from './features/transactions/transactions';

export const routes: Routes = [
  { path: 'register', component: RegistrationPage },
  {
    path: 'login',
    component: Loginpage,
    canActivate: [loginGuard]
  },

  {
    path: 'mainpage',
    component: Mainpage,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: Dashboard },
      { path: 'customer/:id/assets', component: CustomerAssets },
      { path: 'customer/:id/add-car', component: AddCar },
      { path: 'customer/:id/add-house', component: AddHouse },
      { path: 'customer/:id/edit-car/:carId', component: EditCar },
      { path: 'customer/:id/edit-house/:houseId', component: EditHouse },
      { path: 'send-email', component: SendEmail },
      { path: 'settings', component: Settings },
      { path: 'accounts', component: UsersDisplay },
      { path: 'customers', component: CustomerDisplay },
      { path: 'transactions', component: Transactions },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },
  { path: '', component: Landingpage },
  { path: 'not-found', component: NotFound },
  { path: 'unauthorized', component: Unauthorized },
  { path: '**', redirectTo: 'not-found' }
];