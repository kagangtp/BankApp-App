import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { TranslateService } from '@ngx-translate/core';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
    const router = inject(Router);
    const toastr = inject(ToastrService);
    const translate = inject(TranslateService);

    return next(req).pipe(
        catchError((error) => {
            switch (error.status) {
                case 400:
                    const validationMsg = error.error?.message;
                    if (validationMsg) {
                        toastr.warning(validationMsg, translate.instant('TOAST.VALIDATION_ERROR'));
                    } else {
                        toastr.warning(
                            translate.instant('TOAST.ERROR_400'),
                            translate.instant('TOAST.ERROR')
                        );
                    }
                    break;

                case 401:
                    toastr.error(
                        translate.instant('TOAST.ERROR_401'),
                        translate.instant('TOAST.ERROR')
                    );
                    router.navigate(['/unauthorized']);
                    break;

                case 403:
                    toastr.warning(
                        translate.instant('TOAST.ERROR_403'),
                        translate.instant('TOAST.ERROR')
                    );
                    router.navigate(['/unauthorized']);
                    break;

                case 404:
                    toastr.error(
                        translate.instant('TOAST.ERROR_404'),
                        translate.instant('TOAST.ERROR')
                    );
                    router.navigate(['/not-found']);
                    break;

                case 500:
                    toastr.error(
                        translate.instant('TOAST.ERROR_500'),
                        translate.instant('TOAST.ERROR')
                    );
                    break;

                case 0:
                    toastr.error(
                        translate.instant('TOAST.ERROR_NETWORK'),
                        translate.instant('TOAST.ERROR')
                    );
                    break;
            }

            return throwError(() => error);
        })
    );
};
