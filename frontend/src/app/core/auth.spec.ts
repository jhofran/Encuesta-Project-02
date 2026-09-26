import { looksLikeJwt, readClaim } from './auth';

const base64Url = (value: object) =>
  btoa(JSON.stringify(value)).replaceAll('+', '-').replaceAll('/', '_').replace(/=+$/, '');

const jwt = (payload: object) => `${base64Url({ alg: 'none' })}.${base64Url(payload)}.sig`;

describe('auth helpers', () => {
  it('lee el claim sub de un JWT', () => {
    expect(readClaim(jwt({ sub: 'user-1' }), 'sub')).toBe('user-1');
  });

  it('devuelve null si el token está mal formado', () => {
    expect(readClaim('no-es-un-jwt', 'sub')).toBeNull();
  });

  it('reconoce el formato de un JWT', () => {
    expect(looksLikeJwt(jwt({ sub: 'a' }))).toBe(true);
    expect(looksLikeJwt('hola mundo')).toBe(false);
  });
});
