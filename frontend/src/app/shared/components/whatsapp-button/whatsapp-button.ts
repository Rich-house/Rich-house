import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import {
  buildProductInquiryMessage,
  buildWhatsAppUrl,
  publicSiteSettings,
} from '../../../core/config/site-settings.config';

@Component({
  selector: 'app-whatsapp-button',
  imports: [CommonModule],
  templateUrl: './whatsapp-button.html',
  styleUrl: './whatsapp-button.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WhatsAppButtonComponent {
  @Input() appearance: 'primary' | 'subtle' = 'primary';
  @Input() ariaLabel?: string;
  @Input() block = false;
  @Input() label = 'Chat on WhatsApp';
  @Input() message?: string;
  @Input() productName?: string | null;
  @Input() size: 'default' | 'compact' = 'default';
  @Input() variant: 'floating' | 'inline' = 'inline';

  get href(): string | null {
    return buildWhatsAppUrl(
      publicSiteSettings.WhatsAppNumber,
      this.message || buildProductInquiryMessage(this.productName),
    );
  }

  get resolvedAriaLabel(): string {
    return (
      this.ariaLabel
      || (this.productName
        ? `Ask Rich House about ${this.productName} on WhatsApp`
        : 'Message Rich House on WhatsApp')
    );
  }
}
