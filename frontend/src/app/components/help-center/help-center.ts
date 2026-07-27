import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  buildWhatsAppUrl,
  publicSiteSettings,
  richHouseBrand,
} from '../../core/config/site-settings.config';

@Component({
  selector: 'app-help-center',
  imports: [CommonModule, RouterLink],
  templateUrl: './help-center.html',
  styleUrl: './help-center.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HelpCenter {
  readonly brand = richHouseBrand;
  readonly publicSiteSettings = publicSiteSettings;
  readonly whatsAppUrl = buildWhatsAppUrl(publicSiteSettings.WhatsAppNumber);

  readonly channels = [
    {
      label: 'Facebook',
      icon: 'bi-facebook',
      url: publicSiteSettings.FacebookUrl,
      copy: 'Use Facebook for collection updates, showroom-style posts, and direct customer conversations.',
    },
    {
      label: 'Instagram',
      icon: 'bi-instagram',
      url: publicSiteSettings.InstagramUrl,
      copy: 'Use Instagram for styling inspiration, arrivals, and visual updates from Rich House.',
    },
  ] as const;

  readonly helpTopics = [
    'Choosing the right category for an occasion or dress code.',
    'Checking availability, offer visibility, and current product presentation.',
    'Getting a clearer feel for sizes before placing an order.',
  ] as const;

  readonly contactNotes = [
    'Include the product name or route if your question is about a specific piece.',
    'If you already placed an order, include the order reference so support can reply faster later.',
    'Use the official Rich House social profiles listed below so your message reaches the right channel.',
  ] as const;
}
