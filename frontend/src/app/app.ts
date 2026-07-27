import { CommonModule } from '@angular/common';
import { Component, DestroyRef, computed, inject } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRouteSnapshot, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { Footer } from './components/footer/footer';
import { Header } from './components/header/header';
import { RouteSeoConfig, SeoService } from './core/services/seo.service';
import { WhatsAppButtonComponent } from './shared/components/whatsapp-button/whatsapp-button';

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet, Header, Footer, WhatsAppButtonComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly seoService = inject(SeoService);

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map(() => this.router.url),
      startWith(this.router.url),
    ),
    { initialValue: this.router.url },
  );

  readonly showCustomerChrome = computed(
    () => !/^\/(?:dashboard|login|register|confirmemail)(?:\/|$)/.test(this.currentUrl()),
  );

  constructor() {
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        startWith(null),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.applyRouteSeo();
      });
  }

  private applyRouteSeo(): void {
    const activeRoute = this.getDeepestRoute(this.router.routerState.snapshot.root);
    const seoConfig = (activeRoute.data['seo'] as RouteSeoConfig | undefined) ?? {};

    this.seoService.applyRouteSeo(
      {
        ...seoConfig,
        title: activeRoute.title || seoConfig.title,
      },
      this.router.url,
    );
  }

  private getDeepestRoute(route: ActivatedRouteSnapshot): ActivatedRouteSnapshot {
    let currentRoute = route;

    while (currentRoute.firstChild) {
      currentRoute = currentRoute.firstChild;
    }

    return currentRoute;
  }
}
