import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { AdminCatalogService } from '../../../core/services/admin-catalog';
import { describeApiError } from '../../../core/utils/http-error';
import { AdminDashboardSummary, AdminRecentItem } from '../../../shared/models/admin.models';
import { resolveImageUrl, productPlaceholderImage } from '../../../core/utils/image-url';

@Component({
  selector: 'app-admin-overview',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './overview.html',
  styleUrl: './overview.css',
})
export class AdminOverviewComponent {
  private readonly adminCatalogService = inject(AdminCatalogService);
  private readonly cdr = inject(ChangeDetectorRef);

  summary: AdminDashboardSummary | null = null;
  loading = true;
  errorMessage = '';
  readonly fallbackImage = productPlaceholderImage;

  constructor() {
    this.loadSummary();
  }

  loadSummary(): void {
    this.loading = true;
    this.errorMessage = '';

    this.adminCatalogService.getDashboardSummary().subscribe({
      next: (summary) => {
        if (!summary) {
          this.summary = null;
          this.errorMessage = 'The dashboard overview returned no data.';
          this.loading = false;
          return;
        }

        this.summary = {
          ...summary,
          recentOffers: summary.recentOffers ?? [],
          recentProducts: summary.recentProducts ?? [],
        };
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = describeApiError(error).message;
        this.cdr.markForCheck();
      },
    });
  }

  resolveImage(path?: string | null): string {
    return resolveImageUrl(path);
  }

  trackByRecentId(_index: number, item: AdminRecentItem): number {
    return item.id;
  }
}
