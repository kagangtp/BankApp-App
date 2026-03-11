import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CarService } from '../../core/services/carService';
import { ToastrService } from 'ngx-toastr';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
    selector: 'app-edit-car',
    standalone: true,
    imports: [CommonModule, FormsModule, TranslateModule],
    templateUrl: './edit-car.html',
    styleUrl: './edit-car.css',
})
export class EditCar {
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    private carService = inject(CarService);
    private toastr = inject(ToastrService);
    private translate = inject(TranslateService);

    customerId = 0;
    carId = 0;
    plate = '';
    description = '';
    isSubmitting = false;

    ngOnInit() {
        this.customerId = +this.route.snapshot.params['id'];
        this.carId = +this.route.snapshot.params['carId'];
        this.loadCar();
    }

    loadCar() {
        this.carService.getCarById(this.carId).subscribe(res => {
            if (res.success && res.data) {
                this.plate = res.data.plate;
                this.description = res.data.description || '';
            }
        });
    }

    onSubmit() {
        if (!this.plate.trim()) return;
        this.isSubmitting = true;

        this.carService.updateCar({
            id: this.carId,
            plate: this.plate,
            description: this.description || undefined,
        }).subscribe({
            next: (res) => {
                if (res.success) {
                    this.toastr.success(this.translate.instant('EDIT_CAR_PAGE.SUCCESS'));
                    this.goBack();
                } else {
                    this.toastr.error(this.translate.instant('EDIT_CAR_PAGE.UPDATE_ERROR'));
                    this.isSubmitting = false;
                }
            },
            error: (err) => {
                this.toastr.error(err.error?.message || this.translate.instant('COMMON.ERROR_GENERIC'));
                this.isSubmitting = false;
            }
        });
    }

    goBack() {
        this.router.navigate(['/mainpage/customer', this.customerId, 'assets']);
    }
}
