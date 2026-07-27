import { Directive, HostListener, Input } from '@angular/core';
import { productPlaceholderImage, resolveImageUrl } from '../../core/utils/image-url';

@Directive({
  selector: 'img[appImageFallback]',
  standalone: true,
})
export class ImageFallbackDirective {
  @Input('appImageFallback') fallbackImage = productPlaceholderImage;

  @HostListener('error', ['$event.target'])
  onError(target: EventTarget | null): void {
    if (!(target instanceof HTMLImageElement)) {
      return;
    }

    const fallbackUrl = resolveImageUrl(this.fallbackImage);
    const absoluteFallbackUrl = new URL(fallbackUrl, document.baseURI).href;

    if (target.currentSrc === absoluteFallbackUrl || target.src === absoluteFallbackUrl) {
      return;
    }

    target.src = fallbackUrl;
  }
}
