import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Receipt,
  Transaction,
  TaxSummary,
  UploadReceiptResponse,
  TaxPeriodListResponse,
  TaxPeriodDetailResponse,
  TaxPeriod,
  AppNotification,
  TransactionCategory,
} from '../models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private baseUrl = `${environment.apiUrl}/api`;

  constructor(private http: HttpClient) {}

  uploadReceipt(file: File): Observable<UploadReceiptResponse> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<UploadReceiptResponse>(`${this.baseUrl}/receipts/upload`, formData);
  }

  getReceipts(): Observable<Receipt[]> {
    return this.http.get<Receipt[]>(`${this.baseUrl}/receipts`);
  }

  getTransactions(year?: number, month?: number, category?: string): Observable<Transaction[]> {
    let params = new HttpParams();
    if (year) params = params.set('year', year);
    if (month) params = params.set('month', month);
    if (category) params = params.set('category', category);
    return this.http.get<Transaction[]>(`${this.baseUrl}/transactions`, { params });
  }

  categorizeTransaction(transactionId: string, category: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/transactions/${transactionId}/category`, {
      transactionId,
      category,
    });
  }

  updateTransaction(
    transactionId: string,
    payload: { amount: number; description: string; transactionDate: string; category: TransactionCategory }
  ): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/transactions/${transactionId}`, {
      transactionId,
      ...payload,
    });
  }

  deleteTransaction(transactionId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/transactions/${transactionId}`);
  }

  getTaxSummary(): Observable<TaxSummary> {
    return this.http.get<TaxSummary>(`${this.baseUrl}/tax/summary`);
  }

  getTaxPeriods(): Observable<TaxPeriodListResponse> {
    return this.http.get<TaxPeriodListResponse>(`${this.baseUrl}/taxperiods`);
  }

  getTaxPeriodDetail(id: string): Observable<TaxPeriodDetailResponse> {
    return this.http.get<TaxPeriodDetailResponse>(`${this.baseUrl}/taxperiods/${id}`);
  }

  closeTaxPeriod(id: string): Observable<TaxPeriod> {
    return this.http.post<TaxPeriod>(`${this.baseUrl}/taxperiods/${id}/close`, {});
  }

  lockTaxPeriod(id: string): Observable<TaxPeriod> {
    return this.http.post<TaxPeriod>(`${this.baseUrl}/taxperiods/${id}/lock`, {});
  }

  getNotifications(): Observable<AppNotification[]> {
    return this.http.get<AppNotification[]>(`${this.baseUrl}/notifications`);
  }

  getUnreadNotificationCount(): Observable<{ count: number }> {
    return this.http.get<{ count: number }>(`${this.baseUrl}/notifications/unread-count`);
  }

  markNotificationRead(id: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/notifications/${id}/read`, {});
  }

  markAllNotificationsRead(): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/notifications/read-all`, {});
  }

  seedDemoData(): Observable<{ transactionsCreated: number; totalIncome: number; totalTaxWithheld: number }> {
    return this.http.post<{ transactionsCreated: number; totalIncome: number; totalTaxWithheld: number }>(`${this.baseUrl}/demo/seed`, {});
  }
}