import { Component, OnInit, ChangeDetectionStrategy, inject, DestroyRef } from '@angular/core';
import { CommonModule, DatePipe, CurrencyPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../services/api.service';
import {
  TaxPeriod,
  TaxPeriodListResponse,
  TaxPeriodDetailResponse,
  TaxPeriodTransaction,
} from '../../models';

@Component({
  selector: 'app-tax-history',
  standalone: true,
  imports: [CommonModule, DatePipe, CurrencyPipe],
  templateUrl: './tax-history.component.html',
  styleUrls: ['./tax-history.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaxHistoryComponent implements OnInit {
  periods: TaxPeriod[] = [];
  comparison?: TaxPeriodListResponse['comparison'];
  detail?: TaxPeriodDetailResponse;
  loadingDetailId?: string;
  busyId?: string;
  error = '';

  private destroyRef = inject(DestroyRef);
  private api = inject(ApiService);

  ngOnInit() {
    this.api.getTaxPeriods().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res: TaxPeriodListResponse) => {
        this.periods = res.periods;
        this.comparison = res.comparison;
      },
      error: () => (this.error = 'Failed to load tax history.'),
    });
  }

  statusClass(status: TaxPeriod['status']) {
    switch (status) {
      case 'Locked': return 'badge neutral';
      case 'Closed': return 'badge warning';
      default: return 'badge success';
    }
  }

  toggleDetail(id: string) {
    if (this.loadingDetailId === id) return;
    if (this.detail?.period.id === id) {
      this.detail = undefined;
      return;
    }
    this.loadingDetailId = id;
    this.api.getTaxPeriodDetail(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (d: TaxPeriodDetailResponse) => {
        this.detail = d;
        this.loadingDetailId = undefined;
      },
      error: () => {
        this.loadingDetailId = undefined;
        this.error = 'Failed to load period details.';
      },
    });
  }

  formatPercent(value: number | null | undefined): string {
    if (value === null || value === undefined) return '—';
    return `${value > 0 ? '+' : ''}${value}%`;
  }

  formatCategory(c: string): string {
    return c.replace(/([A-Z])/g, ' $1').trim();
  }

  changeClass(value: number | null | undefined): string {
    if (value === null || value === undefined) return 'muted';
    if (value > 0) return 'up';
    if (value < 0) return 'down';
    return 'flat';
  }

  isBusy(id: string) {
    return this.busyId === id;
  }

  closePeriod(period: TaxPeriod) {
    this.busyId = period.id;
    this.api.closeTaxPeriod(period.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated: TaxPeriod) => {
        this.applyUpdated(updated);
        this.busyId = undefined;
      },
      error: () => {
        this.busyId = undefined;
        this.error = 'Failed to close period.';
      },
    });
  }

  lockPeriod(period: TaxPeriod) {
    this.busyId = period.id;
    this.api.lockTaxPeriod(period.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated: TaxPeriod) => {
        this.applyUpdated(updated);
        this.busyId = undefined;
      },
      error: () => {
        this.busyId = undefined;
        this.error = 'Failed to lock period.';
      },
    });
  }

  private applyUpdated(updated: TaxPeriod) {
    const idx = this.periods.findIndex(p => p.id === updated.id);
    if (idx >= 0) this.periods[idx] = updated;
    if (this.detail?.period.id === updated.id) {
      this.detail = { ...this.detail, period: updated };
    }
  }
}