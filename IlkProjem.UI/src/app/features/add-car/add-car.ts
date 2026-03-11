import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CarService } from '../../core/services/carService';
import { FileService } from '../../core/services/fileService';
import { ToastrService } from 'ngx-toastr';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';

@Component({
    selector: 'app-add-car',
    standalone: true,
    imports: [CommonModule, FormsModule, TranslateModule],
    templateUrl: './add-car.html',
    styleUrl: './add-car.css',
})
export class AddCar {
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    private carService = inject(CarService);
    private fileService = inject(FileService);
    private toastr = inject(ToastrService);
    private translate = inject(TranslateService);

    customerId = 0;
    plate = '';
    description = '';
    selectedFiles: File[] = [];
    isSubmitting = false;

    ngOnInit() {
        this.customerId = +this.route.snapshot.params['id'];
    }

    onFilesSelected(event: Event) {
        const input = event.target as HTMLInputElement;
        if (input.files) {
            this.selectedFiles = Array.from(input.files);
        }
    }

    removeFile(index: number) {
        this.selectedFiles.splice(index, 1);
    }

    onSubmit() {
        if (!this.plate.trim()) return;
        this.isSubmitting = true;

        // 1. Araba oluştur
        this.carService.createCar({
            plate: this.plate,
            description: this.description || undefined,
            customerId: this.customerId,
        }).subscribe({
            next: (res) => {
                if (res.success) {
                    const carId = res.data;

                    // 2. Dosya yoksa direkt geri dön
                    if (this.selectedFiles.length === 0) {
                        this.toastr.success(this.translate.instant('ADD_CAR_PAGE.SUCCESS'));
                        this.goBack();
                        return;
                    }

                    // 3. Dosyaları yükle
                    const uploads = this.selectedFiles.map(f => this.fileService.upload(f));
                    forkJoin(uploads).subscribe({
                        next: (uploadResults) => {
                            // 4. Her dosyayı arabaya bağla
                            const assigns = uploadResults.map((file: any) =>
                                this.fileService.assignOwner(file.id, carId, 'Car')
                            );
                            forkJoin(assigns).subscribe({
                                next: () => {
                                    this.toastr.success(this.translate.instant('ADD_CAR_PAGE.SUCCESS_WITH_FILES'));
                                    this.goBack();
                                },
                                error: () => {
                                    this.toastr.warning(this.translate.instant('ADD_CAR_PAGE.FILE_ASSIGN_ERROR'));
                                    this.goBack();
                                }
                            });
                        },
                        error: () => {
                            this.toastr.warning(this.translate.instant('ADD_CAR_PAGE.FILE_UPLOAD_ERROR'));
                            this.goBack();
                        }
                    });
                } else {
                    this.toastr.error(this.translate.instant('ADD_CAR_PAGE.ADD_ERROR'));
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
