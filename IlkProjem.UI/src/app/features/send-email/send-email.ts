import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MailService } from '../../core/services/mailService';
import { ToastrService } from 'ngx-toastr';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
    selector: 'app-send-email',
    standalone: true,
    imports: [CommonModule, FormsModule, TranslateModule],
    templateUrl: './send-email.html',
    styleUrl: './send-email.css',
})
export class SendEmail {
    private router = inject(Router);
    private mailService = inject(MailService);
    private toastr = inject(ToastrService);
    private translate = inject(TranslateService);

    to = '';
    subject = '';
    body = '';
    isSending = false;

    onSubmit() {
        if (!this.to.trim() || !this.subject.trim() || !this.body.trim()) return;
        this.isSending = true;

        this.mailService.send({
            to: this.to,
            subject: this.subject,
            body: this.body,
        }).subscribe({
            next: () => {
                this.toastr.success(this.translate.instant('SEND_EMAIL_PAGE.SUCCESS'));
                this.resetForm();
                this.isSending = false;
            },
            error: (err) => {
                this.toastr.error(err.error?.message || this.translate.instant('SEND_EMAIL_PAGE.ERROR'));
                this.isSending = false;
            }
        });
    }

    resetForm() {
        this.to = '';
        this.subject = '';
        this.body = '';
    }

    goBack() {
        this.router.navigate(['/mainpage/dashboard']);
    }
}
