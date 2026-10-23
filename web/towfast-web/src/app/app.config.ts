import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { NZ_I18N, pt_BR } from 'ng-zorro-antd/i18n';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import {
  ArrowLeftOutline, ArrowRightOutline, CarOutline, CheckCircleOutline, CheckOutline,
  ClockCircleOutline, CompassOutline, CustomerServiceOutline, EditOutline, EnvironmentOutline,
  EyeInvisibleOutline, EyeOutline, FilterOutline, GlobalOutline, HomeOutline, InboxOutline,
  LogoutOutline, MenuOutline, MoonOutline, PlusOutline, ReloadOutline, RightOutline,
  SafetyCertificateOutline, SettingOutline, StopOutline, TeamOutline, ThunderboltOutline,
  ToolOutline, UserOutline,
} from '@ant-design/icons-angular/icons';
import { routes } from './app.routes';
import { RuntimeConfigService } from './core/runtime-config.service';
import { authInterceptor, correlationInterceptor, problemInterceptor } from './core/http.interceptors';
import { provideApiConfiguration } from './api/generated/api-configuration';

const icons = [ArrowLeftOutline, ArrowRightOutline, CarOutline, CheckCircleOutline, CheckOutline,
  ClockCircleOutline, CompassOutline, CustomerServiceOutline, EditOutline, EnvironmentOutline,
  EyeInvisibleOutline, EyeOutline, FilterOutline, GlobalOutline, HomeOutline, InboxOutline,
  LogoutOutline, MenuOutline, MoonOutline, PlusOutline, ReloadOutline, RightOutline,
  SafetyCertificateOutline, SettingOutline, StopOutline, TeamOutline, ThunderboltOutline,
  ToolOutline, UserOutline];

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([correlationInterceptor, authInterceptor, problemInterceptor])),
    provideAnimationsAsync(),
    { provide: NZ_I18N, useValue: pt_BR },
    provideNzIcons(icons),
    provideApiConfiguration(RuntimeConfigService.config.apiBaseUrl.replace(/\/$/, '')),
  ]
};
