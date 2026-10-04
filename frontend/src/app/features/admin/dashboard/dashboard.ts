import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Auth } from '../../../core/services/auth';
import { richHouseBrand } from '../../../core/config/site-settings.config';

interface AdminNavItem {
  icon: string;
  label: string;
  route: string;
  superAdminOnly?: boolean;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Dashboard {
  private readonly authService = inject(Auth);

  readonly brand = richHouseBrand;
  readonly canManageUsers = this.authService.isSuperAdmin();
  readonly navItems: AdminNavItem[] = [
    { label: 'Overview', route: '/dashboard/overview', icon: 'bi-speedometer2' },
    { label: 'Products', route: '/dashboard/products', icon: 'bi-bag' },
    { label: 'Categories', route: '/dashboard/categories', icon: 'bi-grid' },
    { label: 'Offers', route: '/dashboard/offers', icon: 'bi-percent' },
    { label: 'Users Admin', route: '/dashboard/users', icon: 'bi-people', superAdminOnly: true },
  ];
  sidebarOpen = false;

  toggleSidebar(): void {
    this.sidebarOpen = !this.sidebarOpen;
  }

  closeSidebar(): void {
    this.sidebarOpen = false;
  }

  onLogout(): void {
    this.authService.logout();
  }

  trackByRoute(_index: number, item: AdminNavItem): string {
    return item.route;
  }
}
