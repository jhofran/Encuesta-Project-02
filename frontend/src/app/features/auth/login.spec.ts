import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { AuthService } from '../../core/auth';
import { Login } from './login';

const jwt = `${btoa('{"alg":"none"}').replace(/=+$/, '')}.${btoa('{"sub":"u1"}').replace(/=+$/, '')}.sig`;

describe('Login', () => {
  let http: HttpTestingController;
  let auth: AuthService;
  let navigate: ReturnType<typeof vi.spyOn>;

  const setup = async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    sessionStorage.clear();
    http = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
    auth.logout();
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    const fixture = TestBed.createComponent(Login);
    await fixture.whenStable();
    return { el: fixture.nativeElement as HTMLElement, fixture };
  };

  afterEach(() => http.verify());

  it('el formulario guarda el token pegado y navega al inicio', async () => {
    const { el, fixture } = await setup();
    const textarea = el.querySelector('textarea') as HTMLTextAreaElement;
    textarea.value = jwt;
    textarea.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    (el.querySelector('button[type=submit]') as HTMLButtonElement).click();

    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.userId()).toBe('u1');
    expect(navigate).toHaveBeenCalledWith(['/']);
  });

  it('rechaza un texto que no es un JWT', async () => {
    const { el, fixture } = await setup();
    const textarea = el.querySelector('textarea') as HTMLTextAreaElement;
    textarea.value = 'hola mundo';
    textarea.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    (el.querySelector('button[type=submit]') as HTMLButtonElement).click();

    expect(auth.isAuthenticated()).toBe(false);
  });

  it('«Entrar como admin» pide un token de desarrollo y entra', async () => {
    const { el, fixture } = await setup();

    (el.querySelectorAll('button[type=button]')[1] as HTMLButtonElement).click();

    const req = http.expectOne('/dev/token');
    expect(req.request.body).toEqual({ admin: true });
    req.flush({ token: jwt });

    expect(auth.isAuthenticated()).toBe(true);
    expect(navigate).toHaveBeenCalledWith(['/']);
  });
});
