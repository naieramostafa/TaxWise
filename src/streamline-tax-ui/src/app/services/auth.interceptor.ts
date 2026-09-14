import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { BehaviorSubject, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { HttpRequest, HttpHandlerFn } from '@angular/common/http';

let isRefreshing = false;
const refreshQueue = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.token();

  let authReq = req;
  if (token) {
    authReq = req.clone({ headers: req.headers.set('Authorization', `Bearer ${token}`) });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && token && !req.url.includes('/auth/')) {
        return handleRefresh(auth, router, next, req, error);
      }
      return throwError(() => error);
    })
  );
};

function handleRefresh(authService: AuthService, router: Router, next: HttpHandlerFn, originalReq: HttpRequest<unknown>, originalError: HttpErrorResponse) {

  if (isRefreshing) {
    return refreshQueue.pipe(
      filter((t): t is string => !!t),
      take(1),
      switchMap((newToken) =>
        next(originalReq.clone({ headers: originalReq.headers.set('Authorization', `Bearer ${newToken}`) }))
      )
    );
  }

  isRefreshing = true;
  refreshQueue.next(null);

  const refresh$ = authService.refreshTokens();
  if (!refresh$) {
    isRefreshing = false;
    authService.logout();
    router.navigate(['/login']);
    return throwError(() => originalError);
  }

  return refresh$.pipe(
    switchMap((r) => {
      isRefreshing = false;
      refreshQueue.next(r.token);
      return next(originalReq.clone({ headers: originalReq.headers.set('Authorization', `Bearer ${r.token}`) }));
    }),
    catchError((err) => {
      isRefreshing = false;
      authService.logout();
      router.navigate(['/login']);
      return throwError(() => err);
    })
  );
}
