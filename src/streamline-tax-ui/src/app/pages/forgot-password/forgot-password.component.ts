import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [FormsModule, RouterLink, CommonModule],
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.css'],
})
export class ForgotPasswordComponent {
  email = '';
  loading = false;
  error = '';
  sent = '';

  constructor(private auth: AuthService) {}

  onSubmit() {
    this.loading = true;
    this.error = '';
    this.sent = '';
    this.auth.requestPasswordReset(this.email).subscribe({
      next: (res: any) => { this.sent = res.message || 'Reset link sent.'; this.loading = false; },
      error: (err) => { this.error = err.error?.error || 'Request failed'; this.loading = false; }
    });
  }
}