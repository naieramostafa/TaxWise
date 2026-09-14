import { Component, ChangeDetectionStrategy, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../services/auth.service';
import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingsComponent {
  name = '';
  saving = false;
  message = '';
  messageType: 'success' | 'error' = 'success';

  private destroyRef = inject(DestroyRef);
  auth = inject(AuthService);
  theme = inject(ThemeService);
  private timeoutId?: ReturnType<typeof setTimeout>;

  constructor() {
    this.name = this.auth.displayName();
  }

  saveName() {
    if (!this.name.trim()) {
      this.showMessage('Name cannot be empty.', 'error');
      return;
    }
    this.saving = true;
    this.auth.updateProfile(this.name.trim()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.saving = false;
        this.showMessage('Name updated successfully.', 'success');
      },
      error: (err) => {
        this.saving = false;
        this.showMessage(err.error?.error || 'Failed to update name.', 'error');
      },
    });
  }

  setTheme(theme: 'light' | 'dark') {
    if (theme !== this.theme.theme()) {
      this.theme.toggle();
    }
  }

  private showMessage(msg: string, type: 'success' | 'error') {
    this.message = msg;
    this.messageType = type;
    if (this.timeoutId) clearTimeout(this.timeoutId);
    this.timeoutId = setTimeout(() => (this.message = ''), 3500);
  }
}