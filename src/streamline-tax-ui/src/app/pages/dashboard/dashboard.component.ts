import { Component, OnInit, ChangeDetectionStrategy, inject, DestroyRef, signal } from '@angular/core';
import { CommonModule, CurrencyPipe, PercentPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../services/api.service';
import { TaxSummary } from '../../models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, PercentPipe],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent implements OnInit {
  summary?: TaxSummary;
  showSeedBanner = true;
  seeding = signal(false);
  loading = signal(true);
  error = signal<string | null>(null);

  private destroyRef = inject(DestroyRef);
  private api = inject(ApiService);

  ngOnInit() {
    this.loading.set(true);
    this.error.set(null);
    this.api.getTaxSummary().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: s => {
        this.summary = s;
        if ((s.transactionCount ?? 0) > 0) this.showSeedBanner = false;
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load dashboard data');
        this.loading.set(false);
      }
    });
  }

  seedDemoData() {
    this.seeding.set(true);
    this.api.seedDemoData().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.seeding.set(false);
        this.showSeedBanner = false;
        this.ngOnInit();
      },
      error: () => { this.seeding.set(false); }
    });
  }
}
