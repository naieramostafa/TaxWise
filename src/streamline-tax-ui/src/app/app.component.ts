import { Component, OnInit, OnDestroy } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from './services/auth.service';
import { ThemeService } from './services/theme.service';
import { ApiService } from './services/api.service';
import { SignalRService } from './services/signalr.service';
import { AppNotification } from './models';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
})
export class AppComponent implements OnInit, OnDestroy {
  notifications: AppNotification[] = [];
  unreadCount = 0;
  bellOpen = false;
  private subscription?: Subscription;

  constructor(
    public auth: AuthService,
    public theme: ThemeService,
    private api: ApiService,
    private signalr: SignalRService
  ) {}

  ngOnInit() {
    this.subscription = this.signalr.notificationCreated$.subscribe(n => {
      this.notifications = [n, ...this.notifications];
      if (!n.isRead) this.unreadCount++;
    });

    if (this.auth.token()) {
      this.refreshNotifications();
    }
  }

  refreshNotifications() {
    this.api.getNotifications().subscribe(ns => (this.notifications = ns));
    this.api.getUnreadNotificationCount().subscribe(r => (this.unreadCount = r.count));
  }

  toggleBell() {
    this.bellOpen = !this.bellOpen;
    if (this.bellOpen) this.refreshNotifications();
  }

  markRead(n: AppNotification) {
    if (n.isRead) return;
    this.api.markNotificationRead(n.id).subscribe(() => {
      n.isRead = true;
      this.unreadCount = Math.max(0, this.unreadCount - 1);
    });
  }

  markAllRead() {
    this.api.markAllNotificationsRead().subscribe(() => {
      this.notifications.forEach(n => (n.isRead = true));
      this.unreadCount = 0;
    });
  }

  notificationIcon(type: AppNotification['type']): string {
    switch (type) {
      case 'ReceiptProcessed': return 'ok';
      case 'ReceiptProcessingFailed': return 'fail';
      case 'DuplicateReceipt': return 'duplicate';
      case 'TaxPeriodLocked': return 'lock';
      case 'WeeklyCategorization': return 'reminder';
      case 'TaxDeadlineApproaching': return 'deadline';
      case 'TaxDeadlineOverdue': return 'deadline';
      default: return 'info';
    }
  }

  timeAgo(iso: string): string {
    const then = new Date(iso).getTime();
    const diff = Date.now() - then;
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return 'just now';
    if (mins < 60) return `${mins}m ago`;
    const hours = Math.floor(mins / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    return `${days}d ago`;
  }

  ngOnDestroy() {
    this.subscription?.unsubscribe();
  }
}