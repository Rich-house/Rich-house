import { Directive, HostListener, Input } from '@angular/core';
import { productPlaceholderImage, resolveImageUrl } from '../../core/utils/image-url';

@Directive({
  selector: 'img[appImageFallback]',
  standalone: true,
})
export class ImageFallbackDirective {
  @Input('appImageFallback') fallbackImage: string | readonly string[] = productPlaceholderImage;
  private readonly attemptedUrls = new Set<string>();

  @HostListener('error', ['$event.target'])
  onError(target: EventTarget | null): void {
    if (!(target instanceof HTMLImageElement)) {
      return;
    }

    const configuredFallbacks = Array.isArray(this.fallbackImage)
      ? this.fallbackImage
      : [this.fallbackImage];
    const candidates = [...configuredFallbacks, productPlaceholderImage]
      .map((candidate) => resolveImageUrl(candidate))
      .filter((candidate, index, values) => values.indexOf(candidate) === index);
    const currentUrl = target.currentSrc || target.src;
    this.attemptedUrls.add(currentUrl);

    for (const candidate of candidates) {
      const absoluteCandidateUrl = new URL(candidate, document.baseURI).href;
      if (!this.attemptedUrls.has(absoluteCandidateUrl)) {
        this.attemptedUrls.add(absoluteCandidateUrl);
        target.removeAttribute('srcset');
        target.src = candidate;
        return;
      }
    }
  }
}
