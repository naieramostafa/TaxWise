import { Component, OnInit, ChangeDetectionStrategy, inject, DestroyRef } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../services/api.service';
import { Receipt } from '../../models';

@Component({
  selector: 'app-receipts',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './receipts.component.html',
  styleUrls: ['./receipts.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReceiptsComponent implements OnInit {
  receipts: Receipt[] = [];
  uploading = false;

  private destroyRef = inject(DestroyRef);
  private api = inject(ApiService);

  ngOnInit() {
    this.loadReceipts();
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    const files = event.dataTransfer?.files;
    if (files?.length) this.upload(files[0]);
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) this.upload(input.files[0]);
  }

  private upload(file: File) {
    this.uploading = true;
    this.api.uploadReceipt(file).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.uploading = false; this.loadReceipts(); },
      error: () => { this.uploading = false; }
    });
  }

  private loadReceipts() {
    this.api.getReceipts().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(r => this.receipts = r);
  }
}