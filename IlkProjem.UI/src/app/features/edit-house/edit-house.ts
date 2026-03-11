import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HouseService } from '../../core/services/houseService';
import { ToastrService } from 'ngx-toastr';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
    selector: 'app-edit-house',
    standalone: true,
    imports: [CommonModule, FormsModule, TranslateModule],
    templateUrl: './edit-house.html',
    styleUrl: './edit-house.css',
})
export class EditHouse {
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    private houseService = inject(HouseService);
    private toastr = inject(ToastrService);
    private translate = inject(TranslateService);

    customerId = 0;
    houseId = 0;
    address = '';
    description = '';
    isSubmitting = false;

    ngOnInit() {
        this.customerId = +this.route.snapshot.params['id'];
        this.houseId = +this.route.snapshot.params['houseId'];
        this.loadHouse();
    }

    loadHouse() {
        this.houseService.getHouseById(this.houseId).subscribe(res => {
            if (res.success && res.data) {
                this.address = res.data.address;
                this.description = res.data.description || '';
            }
        });
    }

    onSubmit() {
        if (!this.address.trim()) return;
        this.isSubmitting = true;

        this.houseService.updateHouse({
            id: this.houseId,
            address: this.address,
            description: this.description || undefined,
        }).subscribe({
            next: (res) => {
                if (res.success) {
                    this.toastr.success(this.translate.instant('EDIT_HOUSE_PAGE.SUCCESS'));
                    this.goBack();
                } else {
                    this.toastr.error(this.translate.instant('EDIT_HOUSE_PAGE.UPDATE_ERROR'));
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
