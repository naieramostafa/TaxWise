import { Component, OnInit, ChangeDetectionStrategy, inject, DestroyRef } from '@angular/core';
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
  seeding = false;

  private destroyRef = inject(DestroyRef);
  private api = inject(ApiService);

  ngOnInit() {
    this.api.getTaxSummary().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(s => {
      this.summary = s;
      if ((s.transactionCount ?? 0) > 0) this.showSeedBanner = false;
    });
  }

  seedDemoData() {
    this.seeding = true;
    this.api.seedDemoData().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.seeding = false;
        this.showSeedBanner = false;
        this.ngOnInit();
      },
      error: () => { this.seeding = false; }
    });
  }
}