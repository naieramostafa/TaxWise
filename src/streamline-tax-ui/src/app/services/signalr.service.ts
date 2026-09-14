import { Injectable, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject, filter } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';
import { AppNotification } from '../models';

@Injectable({ providedIn: 'root' })
export class SignalRService implements OnDestroy {
  private hubConnection!: signalR.HubConnection;
  public notificationCreated$ = new Subject<AppNotification>();

  constructor(private auth: AuthService) {
    if (!this.auth.token()) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/tax-notifications`, {
        accessTokenFactory: () => this.auth.token() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('NotificationCreated', (notification: AppNotification) => {
      this.notificationCreated$.next(notification);
    });

    this.hubConnection.start().catch(err => console.error('SignalR error:', err));
  }

  ngOnDestroy() {
    this.hubConnection?.stop();
  }
}