import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError, retry, timer } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    retry({
      count: 2,
      delay: (error, retryCount) => {
        if (error.status === 401 || error.status === 403 || error.status >= 500) {
          return timer(1000 * retryCount);
        }
        return throwError(() => error);
      }
    }),
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        localStorage.clear();
        router.navigate(['/login']);
      } else if (error.status === 0) {
        console.error('Network error - server may be unreachable');
      } else if (error.status >= 500) {
        console.error('Server error:', error.message);
      }
      return throwError(() => error);
    })
  );
};
