import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms'; // 1. Bunları ekle
import { CustomerService } from '../../core/services/customerService';
import { Customer } from '../../core/models/customer';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { Subject, Subscription } from 'rxjs';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { CustomerParams } from '../../core/models/params/customer-params';
import { ExportService } from '../../core/services/exportService';
import { UTextBoxComponent } from '../u-text-box/u-text-box';

@Component({
  selector: 'app-customer-display',
  standalone: true, // Standalone component yapısı
  imports: [CommonModule, CurrencyPipe, DatePipe, ReactiveFormsModule, TranslateModule], // 2. ReactiveFormsModule mutlaka burada olmalı
  templateUrl: './customer-display.html',
  styleUrl: './customer-display.css',
})
export class CustomerDisplay implements OnInit, OnDestroy {

  customerService = inject(CustomerService);
  exportService = inject(ExportService);
  private fb = inject(FormBuilder); // 3. FormBuilder'ı inject et
  private toastr = inject(ToastrService);
  private translate = inject(TranslateService);
  private router = inject(Router);

  customers: Customer[] = [];
  displayCustomers: Customer[] = [];
  updateForm!: FormGroup; // 4. Hata veren değişken tam olarak bu
  isModalOpen = false;
  selectedCustomerId: number | null = null;

  //params
  params: CustomerParams = {
    searchTerm: '',
    sort: 'id', // Cursor pagination works best with 'id'
    pageSize: 10,
    lastId: 0, // Start from the very beginning
    pageIndex: 0
  };

  history: number[] = [];

  private searchSubject = new Subject<string>();
  private searchSubscription?: Subscription;

  ngOnInit() {
    this.loadCustomers();
    this.initUpdateForm(); // Formu başlat
    this.searchSubscription = this.searchSubject.pipe(
      debounceTime(400),        // Wait 400ms after the last keystroke
      distinctUntilChanged()    // Only search if the value actually changed
    ).subscribe(searchTerm => {
      this.executeSearch(searchTerm);
    });
  }

  ngOnDestroy() {
    // Clean up to prevent memory leaks
    this.searchSubscription?.unsubscribe();
  }

  // HTML'deki [formGroup]="updateForm" ile eşleşen yapı
  initUpdateForm() {
    this.updateForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      balance: [0, [Validators.required]],
      tcKimlikNo: ['', [Validators.pattern('^[1-9]{1}[0-9]{10}$')]]
    });
  }

  loadCustomers() {
    // Pass the central params object to the service
    this.customerService.getCustomers(this.params).subscribe(response => {
      if (response.success) {
        // For 100k records, we only show what the backend sends us
        this.displayCustomers = response.data;

        // If you are doing "Load More" (Infinite Scroll):
        // this.displayCustomers = [...this.displayCustomers, ...response.data];
      } else {
        console.error(response.message);
      }
    });
  }

  onEdit(customer: Customer) {
    this.selectedCustomerId = customer.id;
    this.isModalOpen = true;

    // Formun içine verileri basıyoruz
    this.updateForm.patchValue({
      name: customer.name,
      email: customer.email,
      balance: customer.balance,
      tcKimlikNo: customer.tcKimlikNo
    });
  }

  closeModal() {
    this.isModalOpen = false;
    this.updateForm.reset();
  }

  onUpdate() {
    if (this.updateForm.valid && this.selectedCustomerId) {
      const updatedCustomer = {
        id: this.selectedCustomerId,
        ...this.updateForm.value
      };

      // FIX: Remove 'this.selectedCustomerId' as the first argument
      this.customerService.updateCustomer(updatedCustomer).subscribe({
        next: (response) => {
          if (response.success) {
            this.closeModal();
            this.loadCustomers();
            this.toastr.success(this.translate.instant('TOAST.CUSTOMER_UPDATED'), this.translate.instant('TOAST.SUCCESS'));
          } else {
            this.toastr.error(response.message, this.translate.instant('TOAST.ERROR'));
          }
        },
        error: (err) => this.toastr.error(err.message, this.translate.instant('TOAST.UPDATE_ERROR'))
      });
    }
  }

  onDelete(id: number) {
    if (confirm('Emin misiniz?')) {
      this.customerService.deleteCustomer(id).subscribe(response => {
        if (response.success) {
          this.loadCustomers();
          this.toastr.success(this.translate.instant('TOAST.CUSTOMER_DELETED'), this.translate.instant('TOAST.SUCCESS'));
        } else {
          this.toastr.error(response.message, this.translate.instant('TOAST.ERROR'));
        }
      });
    }
  }

  //this is for add modal
  isAddModalOpen = false;


  onAdd() {
    this.isAddModalOpen = true;
    this.updateForm.reset({
      name: '',
      email: '',
      balance: 0, // <--- Bunu eklemezsen form 'invalid' kalır!
      tcKimlikNo: ''
    });
  }



  onCreate() {
    if (this.updateForm.valid) {
      const { name, email } = this.updateForm.value;
      const newCustomer = { name, email };

      this.customerService.createCustomer(newCustomer).subscribe({
        next: (response) => {
          if (response.success) {
            this.isAddModalOpen = false;
            this.loadCustomers();
            this.toastr.success(this.translate.instant('TOAST.CUSTOMER_ADDED'), this.translate.instant('TOAST.SUCCESS'));
          } else {
            this.toastr.error(response.message, this.translate.instant('TOAST.ERROR'));
          }
        },
        error: (err) => this.toastr.error(err.message, this.translate.instant('TOAST.ADD_ERROR'))
      });
    }
  }

  onSearchInput(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.searchSubject.next(value); // This feeds the debounce pipeline
  }

  executeSearch(searchTerm: string) {
    this.params.searchTerm = searchTerm;
    this.params.lastId = 0;
    this.history = []; // Reset history on new search
    this.loadCustomers();
  }
  //pagination with cursor
  loadMore() {
    if (this.displayCustomers.length > 0) {
      // Take the ID of the last item in the list
      const lastItem = this.displayCustomers[this.displayCustomers.length - 1];
      this.params.lastId = lastItem.id;

      this.customerService.getCustomers(this.params).subscribe(response => {
        if (response.success) {
          // APPEND the new data to the existing list
          this.displayCustomers = [...this.displayCustomers, ...response.data];
        }
      });
    }
  }

  goNext() {
    if (this.displayCustomers.length > 0) {
      // 1. Save current cursor to history so we can go back
      this.history.push(this.params.lastId);

      // 2. Set the new cursor to the last ID on the current screen
      const lastItem = this.displayCustomers[this.displayCustomers.length - 1];
      this.params.lastId = lastItem.id;

      this.loadCustomers();
    }
  }

  goPrevious() {
    if (this.history.length > 0) {
      // 1. Pop the previous cursor from our history stack
      const previousId = this.history.pop();

      // 2. Set lastId and reload
      this.params.lastId = previousId ?? 0;
      this.loadCustomers();
    }
  }

  onPageSizeChange(event: Event) {
    const newSize = +(event.target as HTMLSelectElement).value;
    this.params.pageSize = newSize;
    this.params.lastId = 0;   // Başa dön
    this.history = [];         // Geçmişi temizle
    this.loadCustomers();
  }

  exportExcel() {
    this.exportService.getCustomersExcel(this.params).subscribe({
      next: (blob) => {
        const fileName = `Musteri_Listesi_${new Date().toLocaleDateString()}.xlsx`;
        this.exportService.saveAsExcel(blob, fileName);
      },
      error: (err) => {
        console.error("Excel indirilirken bir hata oluştu:", err);
      }
    });
  }

  // onUTextSearch(value: string) {
  //   this.params.searchTerm = value;
  //   this.searchSubject.next(value); // Debounce (400ms) burada devreye girer
  // }

  goToAssets(customerId: number) {
    this.router.navigate(['/mainpage/customer', customerId, 'assets']);
  }

}



