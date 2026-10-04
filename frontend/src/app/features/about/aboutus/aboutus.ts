import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '../../../core/services/seo.service';
import {
  richHouseBrand,
  richHouseStoreLocations,
} from '../../../core/config/site-settings.config';
import {
  productPlaceholderImage,
  resolveImageUrl,
  resolveResponsiveProductImageUrl,
} from '../../../core/utils/image-url';
import { ImageFallbackDirective } from '../../../shared/directives/image-fallback.directive';

@Component({
  selector: 'app-aboutus',
  imports: [CommonModule, RouterLink, ImageFallbackDirective],
  templateUrl: './aboutus.html',
  styleUrl: './aboutus.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Aboutus {
  private readonly seoService = inject(SeoService);

  readonly fallbackImage = productPlaceholderImage;
  readonly heroImageUrl = 'assets/hero/rich-house-suit-hero-poster.webp';
  readonly heroMobileImageSrcset =
    'assets/hero/rich-house-suit-hero-mobile-820.webp?v=20261004 820w, ' +
    'assets/hero/rich-house-suit-hero-mobile.webp?v=20260729 1080w';
  readonly storyImageUrl = resolveImageUrl(
    '/images/products/optimized/beige-three-piece-suit-with-contrast-waistcoat-f11841b1ae8d4e21ac1ad9d2bfd844f3.webp',
  );
  readonly storyImageSrcset =
    `${resolveResponsiveProductImageUrl('/images/products/optimized/beige-three-piece-suit-with-contrast-waistcoat-f11841b1ae8d4e21ac1ad9d2bfd844f3.webp')} 720w, ` +
    `${this.storyImageUrl} 1024w`;
  readonly experienceImageUrl = resolveImageUrl(
    '/images/products/optimized/black-contrast-trim-three-piece-tuxedo-e6feccff96454f27af795ec9ecec0f4a.webp',
  );
  readonly experienceImageSrcset =
    `${resolveResponsiveProductImageUrl('/images/products/optimized/black-contrast-trim-three-piece-tuxedo-e6feccff96454f27af795ec9ecec0f4a.webp')} 720w, ` +
    `${this.experienceImageUrl} 1024w`;

  readonly values = [
    {
      number: '01',
      title: 'Craft',
      copy: 'Refined construction and close attention to the details that give tailoring its presence.',
    },
    {
      number: '02',
      title: 'Fit',
      copy: 'Confident, considered proportions that make formal and everyday dressing feel composed.',
    },
    {
      number: '03',
      title: 'Versatility',
      copy: 'Menswear that moves naturally from work and celebrations to elevated everyday occasions.',
    },
    {
      number: '04',
      title: 'Service',
      copy: 'Personal assistance through Rich House stores and the existing WhatsApp contact experience.',
    },
  ] as const;

  readonly storeLocations = richHouseStoreLocations.map((location, index) => ({
    ...location,
    imageUrl:
      index === 0
        ? 'assets/about/rich-house-storefront-wide.webp'
        : 'assets/about/rich-house-storefront-portrait.webp',
    imageWidth: index === 0 ? 1600 : 1100,
    imageHeight: index === 0 ? 739 : 1528,
    imageAlt: `${location.name} Rich House storefront`,
  }));

  constructor() {
    this.seoService.setPageSeo({
      title: `About | ${richHouseBrand.name}`,
      description:
        'Discover Rich House, an Egyptian menswear brand focused on refined tailoring, confident fit, occasionwear, and elevated everyday dressing.',
      keywords: [
        richHouseBrand.name,
        'Rich House Egypt',
        'about',
        'Egyptian menswear',
        'formalwear',
        'tailoring',
        'suits',
      ],
      breadcrumbs: [
        { name: 'Home', path: '/' },
        { name: 'About', path: '/about' },
      ],
      image: this.heroImageUrl,
    });
  }
}
