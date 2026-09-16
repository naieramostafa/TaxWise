import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, inject, DestroyRef, signal } from '@angular/core';
import { CommonModule, DatePipe, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../services/api.service';
import { Transaction, TransactionCategory, TaxPeriod } from '../../models';

@Component({
  selector: 'app-transactions',
  standalone: true,
  imports: [CommonModule, DatePipe, CurrencyPipe, FormsModule],
  templateUrl: './transactions.component.html',
  styleUrls: ['./transactions.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionsComponent implements OnInit {
  transactions: Transaction[] = [];
  allTransactions: Transaction[] = [];
  categories: TransactionCategory[] = [
    'Uncategorized', 'Salary', 'Freelance', 'BusinessIncome', 'Investment', 'Rental', 'OtherIncome'
  ];
  filter: 'all' | 'uncategorized' = 'all';
  periods: TaxPeriod[] = [];
  editingId?: string;
  editAmount = 0;
  editDescription = '';
  editDate = '';
  error = '';
  deleteConfirmId?: string;
  loading = signal(true);
  saving = signal(false);

  private destroyRef = inject(DestroyRef);

  constructor(private api: ApiService) {}

  ngOnInit() {
    this.load();
    this.api.getTaxPeriods().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => (this.periods = res.periods));
  }

  private load() {
    this.loading.set(true);
    this.api.getTransactions().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => {
        this.allTransactions = res.items;
        this.applyFilter();
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  applyFilter() {
    this.transactions =
      this.filter === 'uncategorized'
        ? this.allTransactions.filter(t => t.category === 'Uncategorized')
        : [...this.allTransactions];
  }

  get uncategorizedCount(): number {
    return this.allTransactions.filter(t => t.category === 'Uncategorized').length;
  }

  setFilter(f: 'all' | 'uncategorized') {
    this.filter = f;
    this.applyFilter();
  }

  periodName(tx: Transaction): string {
    const p = this.periods.find(x => x.id === tx.taxPeriodId);
    return p ? p.name : '';
  }

  periodLocked(tx: Transaction): boolean {
    const p = this.periods.find(x => x.id === tx.taxPeriodId);
    return !!p && p.status === 'Locked';
  }

  onCategoryChange(transactionId: string, category: TransactionCategory) {
    this.saving.set(true);
    this.api.categorizeTransaction(transactionId, category).subscribe({
      next: () => {
        const tx = this.allTransactions.find(t => t.id === transactionId);
        if (tx) tx.category = category;
        this.applyFilter();
        this.saving.set(false);
      },
      error: (e) => {
        this.handleError(e);
        this.saving.set(false);
      },
    });
  }

  startEdit(tx: Transaction) {
    this.editingId = tx.id;
    this.editAmount = tx.amount;
    this.editDescription = tx.description;
    this.editDate = tx.transactionDate.slice(0, 10);
  }

  cancelEdit() {
    this.editingId = undefined;
    this.error = '';
  }

  saveEdit(tx: Transaction) {
    this.saving.set(true);
    this.api
      .updateTransaction(tx.id, {
        amount: this.editAmount,
        description: this.editDescription,
        transactionDate: new Date(this.editDate + 'T00:00:00').toISOString(),
        category: tx.category,
      })
      .subscribe({
        next: () => {
          tx.amount = this.editAmount;
          tx.description = this.editDescription;
          tx.transactionDate = new Date(this.editDate + 'T00:00:00').toISOString();
          this.editingId = undefined;
          this.error = '';
          this.load();
          this.saving.set(false);
        },
        error: (e) => {
          this.handleError(e);
          this.saving.set(false);
        },
      });
  }

  deleteTransaction(tx: Transaction) {
    this.deleteConfirmId = tx.id;
  }

  confirmDelete(tx: Transaction) {
    this.deleteConfirmId = undefined;
    this.saving.set(true);
    this.api.deleteTransaction(tx.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.load();
        this.saving.set(false);
      },
      error: (e) => {
        this.handleError(e);
        this.saving.set(false);
      },
    });
  }

  cancelDelete() {
    this.deleteConfirmId = undefined;
  }

  private handleError(e: unknown) {
    const msg = (e as any)?.error?.error || (e as any)?.message || 'An error occurred';
    this.error = typeof msg === 'string' ? msg : 'An error occurred';
    this.load();
  }

  formatCategory(c: string) {
    return c.replace(/([A-Z])/g, ' $1').trim();
  }
}
