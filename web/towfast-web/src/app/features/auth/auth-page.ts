import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { AuthService } from '../../core/auth.service';
import { DocumentType } from '../../api/generated/models/document-type';
import { digits, isCnpj, isCpf, isPhone } from '../../shared/form-validators';

@Component({
  selector: 'app-auth-page',
  imports: [FormsModule, RouterLink, NzButtonModule, NzIconModule, NzInputModule, NzSelectModule],
  templateUrl: './auth-page.html',
  styleUrl: './auth-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthPage {
  private readonly auth = inject(AuthService); private readonly router = inject(Router); private readonly messages = inject(NzMessageService);
  readonly registering = signal(location.pathname.includes('cadastro'));
  readonly busy = signal(false);
  email = ''; password = ''; name = ''; phone = ''; document = ''; documentType: DocumentType = 'Cpf';
  readonly documentTypes = [{ label: 'CPF', value: 'Cpf' }, { label: 'CNPJ', value: 'Cnpj' }];

  async submit(): Promise<void> {
    if (!this.email || !this.password || (this.registering() && (!this.name || !this.document))) return;
    if (this.registering() && ((this.documentType === 'Cpf' ? !isCpf(this.document) : !isCnpj(this.document)) || (this.phone && !isPhone(this.phone)) || this.password.length < 8)) {
      this.messages.warning('Revise os dados: informe documento, telefone e senha de pelo menos 8 caracteres válidos.');
      return;
    }
    this.busy.set(true);
    try {
      if (this.registering()) await this.auth.register({ email: this.email.trim().toLowerCase(), password: this.password, name: this.name.trim(), phone: digits(this.phone), document: digits(this.document), documentType: this.documentType });
      else await this.auth.login({ email: this.email, password: this.password });
      this.messages.success(this.registering() ? 'Conta criada' : 'Bem-vindo à TowFast');
      await this.router.navigateByUrl('/app');
    } catch {
      this.password = '';
      this.messages.error(`${this.registering() ? 'Não foi possível criar a conta' : 'E-mail ou senha inválidos'}. Confira os dados informados e tente novamente.`);
    } finally { this.busy.set(false); }
  }
}
