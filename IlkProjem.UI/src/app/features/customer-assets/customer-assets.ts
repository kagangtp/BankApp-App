import { Component, inject, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

// Services
import { CarService } from '../../core/services/carService';
import { HouseService } from '../../core/services/houseService';
import { CustomerService } from '../../core/services/customerService';
import { FileService } from '../../core/services/fileService'; // Added FilesService

// Models
import { Car } from '../../core/models/car';
import { House } from '../../core/models/house';
import { Customer } from '../../core/models/customer';

// Environment
import { environment } from '../../../environments/environment.development';

@Component({
    selector: 'app-customer-assets',
    standalone: true,
    imports: [CommonModule, TranslateModule],
    templateUrl: './customer-assets.html',
    styleUrl: './customer-assets.css',
})
export class CustomerAssets implements OnInit {
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    private carService = inject(CarService);
    private houseService = inject(HouseService);
    private customerService = inject(CustomerService);
    private filesService = inject(FileService); // Injected

    @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

    readonly rootUrl = environment.rootUrl;
    private readonly defaultAvatar = 'core/assets/images/default-avatar.png';

    customerId = 0;
    customer: Customer | null = null;
    cars: Car[] = [];
    houses: House[] = [];

    profileImageUrl = this.defaultAvatar;

    // Lightbox
    lightboxImage: string | null = null;
    lightboxAlt = '';

    ngOnInit() {
        this.customerId = +this.route.snapshot.params['id'];
        this.loadCustomer();
        this.loadCars();
        this.loadHouses();
    }

    /**
     * Triggers the hidden file input
     */
    triggerFileUpload() {
        this.fileInput.nativeElement.click();
    }

    /**
     * Handles the file selection and upload sequence
     */
    onFileSelected(event: Event) {
        const input = event.target as HTMLInputElement;
        if (!input.files?.length) return;

        const file = input.files[0];

        // 1. Upload the file to get the Guid from .NET
        this.filesService.upload(file).subscribe({
            next: (uploadRes) => {
                if (uploadRes.success) {
                    const newFileId = uploadRes.data.id; // This is the Guid? ProfileImageId

                    // 2. Assign the file to the current Customer
                    const assignDto = {
                        ownerType: 'Customer',
                        ownerId: this.customerId
                    };

                    this.filesService.assignOwner(newFileId, this.customerId, 'Customer').subscribe({
                        next: (assignRes) => {
                            if (assignRes.success) {
                                // 3. Reload customer to get the new profileImagePath
                                this.loadCustomer();
                            }
                        },
                        error: (err) => console.error('Assignment failed', err)
                    });
                }
            },
            error: (err) => {
                console.error('Upload failed', err);
                this.handleImageError();
            }
        });
    }

    loadCustomer() {
        this.customerService.getCustomerById(this.customerId).subscribe(res => {
            if (res.success) {
                this.customer = res.data;
                this.profileImageUrl = this.customer?.profileImagePath
                    ? `${this.rootUrl}/uploads/${this.customer.profileImagePath}`
                    : this.defaultAvatar;
            }
        });
    }

    loadCars() {
        this.carService.getCarsByCustomer(this.customerId).subscribe(res => {
            if (res.success) this.cars = res.data;
        });
    }

    loadHouses() {
        this.houseService.getHousesByCustomer(this.customerId).subscribe(res => {
            if (res.success) this.houses = res.data;
        });
    }

    goAddCar() {
        this.router.navigate(['/mainpage/customer', this.customerId, 'add-car']);
    }

    goAddHouse() {
        this.router.navigate(['/mainpage/customer', this.customerId, 'add-house']);
    }

    goEditCar(carId: number) {
        this.router.navigate(['/mainpage/customer', this.customerId, 'edit-car', carId]);
    }

    goEditHouse(houseId: number) {
        this.router.navigate(['/mainpage/customer', this.customerId, 'edit-house', houseId]);
    }

    goBack() {
        this.router.navigate(['/mainpage/customers']);
    }

    openLightbox(relativePath: string, alt: string) {
        this.lightboxImage = environment.rootUrl + '/uploads/' + relativePath;
        this.lightboxAlt = alt;
    }

    closeLightbox() {
        this.lightboxImage = null;
    }

    handleImageError() {
        this.profileImageUrl = this.defaultAvatar;
    }
}