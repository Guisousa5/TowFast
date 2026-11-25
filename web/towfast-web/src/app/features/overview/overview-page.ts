import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { forkJoin, timer } from 'rxjs';
import { TowFastApiService } from '../../core/towfast-api.service';
import { SessionService } from '../../core/session.service';
import { ServiceRequestResponse } from '../../api/generated/models/service-request-response';

@Component({selector:'app-overview-page',imports:[RouterLink,DatePipe,NzButtonModule,NzIconModule,NzTagModule],templateUrl:'./overview-page.html',styleUrl:'./overview-page.css',changeDetection:ChangeDetectionStrategy.OnPush})
export class OverviewPage implements OnInit{
  private readonly api=inject(TowFastApiService);private readonly destroy=inject(DestroyRef);readonly session=inject(SessionService);readonly loading=signal(true);readonly requests=signal<ServiceRequestResponse[]>([]);readonly available=signal(0);
  readonly active=computed(()=>this.requests().filter(x=>['Accepted','EnRoute','InProgress'].includes(x.status??'')).length);readonly completed=computed(()=>this.requests().filter(x=>x.status==='Completed').length);readonly pending=computed(()=>this.requests().filter(x=>x.status==='Pending').length);
  ngOnInit():void{
    const poll=timer(0,15000).subscribe(()=>{if(!document.hidden)this.reload()});
    this.destroy.onDestroy(()=>poll.unsubscribe());
  }
  private reload():void{
    if(this.session.role()==='Driver'){
      forkJoin({mine:this.api.requests({pageSize:20}),open:this.api.availableRequests({pageSize:1})}).subscribe({next:r=>{this.requests.set(r.mine.items??[]);this.available.set(r.open.total??0);this.loading.set(false)},error:()=>this.loading.set(false)});
    }else{
      this.api.requests({pageSize:20}).subscribe({next:r=>{this.requests.set(r.items??[]);this.loading.set(false)},error:()=>this.loading.set(false)});
    }
  }
  statusLabel(s?:string){return({Pending:'Aguardando',Accepted:'Aceito',EnRoute:'A caminho',InProgress:'Em atendimento',Completed:'Concluído',Cancelled:'Cancelado'} as Record<string,string>)[s??'']??s;}
  statusColor(s?:string):string{return s==='Completed'?'success':s==='Cancelled'?'error':s==='Pending'?'warning':s==='InProgress'?'processing':'default';}
  formatPrice(value?:number|null):string{return value==null?'Preço não registrado':new Intl.NumberFormat('pt-BR',{style:'currency',currency:'BRL'}).format(value);}
}
