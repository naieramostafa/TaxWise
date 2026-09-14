import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [FormsModule, RouterLink, CommonModule],
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.css'],
})
export class ResetPasswordComponent {
  password = '';
  confirm = '';
  loading = false;
  error = '';
  success = false;

  private email: string | null = null;
  private token: string | null = null;

  constructor(
    private auth: AuthService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.route.queryParams.subscribe(p => {
      this.email = p['email'] ?? null;
      this.token = p['token'] ?? null;
    });
  }

  onSubmit() {
    if (this.password !== this.confirm) {
      this.error = 'Passwords do not match';
      return;
    }
    if (!this.email || !this.token) {
      this.error = 'Invalid or missing reset link.';
      return;
    }
    this.loading = true;
    this.error = '';
    this.auth.resetPassword(this.email, this.token, this.password).subscribe({
      next: () => { this.success = true; this.loading = false; },
      error: (err) => { this.error = err.error?.error || 'Reset failed'; this.loading = false; }
    });
  }
}