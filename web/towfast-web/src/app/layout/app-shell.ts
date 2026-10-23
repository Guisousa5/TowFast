import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';
import { SessionService } from '../core/session.service';
import { AuthService } from '../core/auth.service';

@Component({ selector:'app-shell', imports:[RouterOutlet,RouterLink,RouterLinkActive,NzButtonModule,NzIconModule,NzTooltipModule], templateUrl:'./app-shell.html', styleUrl:'./app-shell.css', changeDetection:ChangeDetectionStrategy.OnPush })
export class AppShell {
  readonly session=inject(SessionService); private readonly auth=inject(AuthService); readonly menuOpen=signal(false);
  readonly roleName=computed(()=>({Admin:'Administrador',Operator:'Operação',Client:'Cliente',Provider:'Prestador',Driver:'Motorista'}[this.session.role() ?? 'Client']));
  readonly isAdmin=computed(()=>['Admin','Operator'].includes(this.session.role() ?? ''));
  readonly isProvider=computed(()=>this.session.role()==='Provider');
  toggleTheme():void { document.documentElement.classList.toggle('app-dark'); localStorage.setItem('towfast.theme',document.documentElement.classList.contains('app-dark')?'dark':'light'); }
  logout():void { void this.auth.logout(); }
}
