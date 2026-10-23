import { TestBed } from '@angular/core/testing';
import { SessionService } from './session.service';

describe('SessionService', () => {
  beforeEach(() => { sessionStorage.clear(); TestBed.configureTestingModule({}); });

  it('stores and clears an authenticated session', () => {
    const session = TestBed.inject(SessionService);
    session.set({ accessToken: 'access', refreshToken: 'refresh', user: { id: '1', email: 'client@towfast.test', role: 'Client', clientId: '2' } });
    expect(session.authenticated()).toBe(true);
    expect(session.role()).toBe('Client');
    expect(sessionStorage.getItem('towfast.refresh-token')).toBe('refresh');
    expect(sessionStorage.getItem('towfast.access-token')).toBeNull();
    session.clear();
    expect(session.authenticated()).toBe(false);
    expect(sessionStorage.length).toBe(0);
  });
});
